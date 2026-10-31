using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Iam.Application.Errors;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Application.Services;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Iam.Domain.Services;
using DataBit.StockIA.Platform.Iam.Resources;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.CommandServices;

/// <summary>
///     Session (Login Controller, Account Lock Service), password (Password
///     Change and Password Reset Services) and team management of Identity
///     &amp; Access Management.
/// </summary>
public class UserCommandService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IHashingService hashingService,
    ExternalNotificationService externalNotificationService,
    IUnitOfWork unitOfWork,
    IRestaurantClock clock,
    IConfiguration configuration,
    ILogger<UserCommandService> logger,
    IStringLocalizer<IamMessages> localizer) : IUserCommandService
{
    public async Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var now = clock.Now;
        var user = await userRepository.FindByEmailAsync(User.NormalizeEmail(command.Email), cancellationToken);
        if (user == null)
            return Result<(User, string)>.Failure(IamError.InvalidCredentials, localizer[nameof(IamError.InvalidCredentials)]);
        if (user.IsLockedAt(now)) return AccountLocked(user, now);

        if (!hashingService.VerifyPassword(command.Password, user.PasswordHash))
        {
            var locked = AccountLockService.RegisterFailedAttempt(user, now);
            userRepository.Update(user);
            await unitOfWork.CompleteAsync(cancellationToken);
            if (locked) return AccountLocked(user, now);
            var remaining = AccountLockService.RemainingAttempts(user);
            return Result<(User, string)>.Failure(IamError.InvalidCredentials, localizer[nameof(IamError.InvalidCredentials)],
                new { remainingAttempts = remaining, maxAttempts = AccountLockService.MaxFailedAttempts });
        }

        if (user.FailedSignInAttempts > 0 || user.LockedUntil != null)
        {
            user.Unlock();
            userRepository.Update(user);
            await unitOfWork.CompleteAsync(cancellationToken);
        }

        return Result<(User, string)>.Success((user, tokenService.GenerateToken(user)));
    }

    /// <summary>The owner of a new restaurant is its first Administrator (Restaurant Registration).</summary>
    public async Task<Result<(User user, string token)>> Handle(CreateOwnerAccountCommand command, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(command.Email);
        if (await userRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken))
            return Result<(User, string)>.Failure(IamError.EmailAlreadyTaken, localizer[nameof(IamError.EmailAlreadyTaken), email]);

        var user = new User(command.FullName, email, hashingService.HashPassword(command.Password), command.RestaurantId, UserRole.Admin);
        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<(User, string)>.Success((user, tokenService.GenerateToken(user)));
    }

    /// <summary>
    ///     Sends the recovery link when the email is registered. The answer is
    ///     the same either way, so nobody can find out which emails exist.
    /// </summary>
    public async Task<Result<PasswordResetRequest>> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(command.Email);
        var masked = PasswordResetService.MaskEmail(email);
        var user = await userRepository.FindByEmailAsync(email, cancellationToken);
        if (user == null) return Result<PasswordResetRequest>.Success(new PasswordResetRequest(masked, null));

        var code = PasswordResetService.NewCode();
        var expiresAt = clock.Now.Add(PasswordResetService.CodeLifetime);
        user.StartPasswordReset(hashingService.HashPassword(code), expiresAt);
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);

        var link = $"{(configuration["Iam:PasswordReset:ResetLinkBaseUrl"] ?? "http://localhost:5173").TrimEnd('/')}" +
                   $"/auth/reset-password?email={Uri.EscapeDataString(email)}&code={code}";
        var subject = localizer["PasswordResetSubject"];
        var body = localizer["PasswordResetBody", user.FullName, link, code, (int)PasswordResetService.CodeLifetime.TotalMinutes];
        var sent = await externalNotificationService.SendEmail(user.RestaurantId, email, subject, body, cancellationToken);
        if (!sent) logger.LogWarning("The recovery email for user {UserId} could not be sent", user.Id);

        var expose = externalNotificationService.IsSimulated &&
                     bool.TryParse(configuration["Iam:PasswordReset:ExposeSimulatedEmail"], out var flag) && flag;
        return Result<PasswordResetRequest>.Success(new PasswordResetRequest(masked,
            expose ? new SimulatedEmail(email, subject, link, code, expiresAt) : null));
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var now = clock.Now;
        var user = await userRepository.FindByEmailAsync(User.NormalizeEmail(command.Email), cancellationToken);
        if (user == null || !PasswordResetService.CanUseCode(user, now))
            return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);

        if (!hashingService.VerifyPassword(command.Code.Trim(), user.PasswordResetCodeHash!))
        {
            user.RegisterFailedPasswordReset();
            userRepository.Update(user);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);
        }

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword));
        user.ClearPasswordReset();
        user.Unlock();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<User>> Handle(UnlockUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null || user.RestaurantId != command.RestaurantId)
            return Result<User>.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        user.Unlock();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<User>.Success(user);
    }

    public async Task<Result<User>> Handle(InviteMemberCommand command, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(command.Email);
        if (await userRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken))
            return Result<User>.Failure(IamError.EmailAlreadyTaken, localizer[nameof(IamError.EmailAlreadyTaken), email]);

        var user = new User(command.FullName, email, hashingService.HashPassword(command.TemporaryPassword), command.RestaurantId, command.Role);
        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<User>.Success(user);
    }

    /// <summary>Name and email only; the password has its own command.</summary>
    public async Task<Result<User>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result<User>.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        var email = User.NormalizeEmail(command.Email);
        if (await userRepository.ExistsByEmailAsync(email, user.Id, cancellationToken))
            return Result<User>.Failure(IamError.EmailAlreadyTaken, localizer[nameof(IamError.EmailAlreadyTaken), email]);

        user.UpdateProfile(command.FullName, email);
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<User>.Success(user);
    }

    /// <summary>Password Change Service: the current password is verified before saving the new one.</summary>
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);
        if (!hashingService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            return Result.Failure(IamError.CurrentPasswordInvalid, localizer[nameof(IamError.CurrentPasswordInvalid)]);
        if (command.CurrentPassword == command.NewPassword)
            return Result.Failure(IamError.SamePassword, localizer[nameof(IamError.SamePassword)]);

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword));
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>The team always keeps at least one Administrator.</summary>
    public async Task<Result<User>> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null || user.RestaurantId != command.RestaurantId)
            return Result<User>.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);
        if (user.Role == command.Role) return Result<User>.Success(user);
        if (user.IsAdmin && await userRepository.CountAdminsAsync(command.RestaurantId, cancellationToken) <= 1)
            return Result<User>.Failure(IamError.LastAdministrator, localizer[nameof(IamError.LastAdministrator)]);

        user.ChangeRole(command.Role);
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<User>.Success(user);
    }

    public async Task<Result> Handle(RemoveMemberCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null || user.RestaurantId != command.RestaurantId)
            return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);
        if (user.Id == command.ActingUserId)
            return Result.Failure(IamError.CannotRemoveYourself, localizer[nameof(IamError.CannotRemoveYourself)]);
        if (user.IsAdmin && await userRepository.CountAdminsAsync(command.RestaurantId, cancellationToken) <= 1)
            return Result.Failure(IamError.LastAdministrator, localizer[nameof(IamError.LastAdministrator)]);

        userRepository.Remove(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    private Result<(User, string)> AccountLocked(User user, DateTimeOffset now) =>
        Result<(User, string)>.Failure(IamError.AccountLocked,
            localizer[nameof(IamError.AccountLocked), AccountLockService.MinutesLeft(user, now)],
            new { lockedUntil = user.LockedUntil, minutesLeft = AccountLockService.MinutesLeft(user, now) });
}
