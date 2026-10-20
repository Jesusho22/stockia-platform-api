using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Iam.Application.CommandServices;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.Errors;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Iam.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.Subscription.Interfaces.Acl;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.CommandServices;

/// <summary>
///     Session (sign-in, sign-up) and team management of the User &amp; Access
///     Management bounded context.
/// </summary>
public class UserCommandService(
    IUserRepository userRepository,
    IRestaurantRepository restaurantRepository,
    ITokenService tokenService,
    IHashingService hashingService,
    IUnitOfWork unitOfWork,
    ISubscriptionContextFacade subscriptionContextFacade,
    IStringLocalizer<IamMessages> localizer) : IUserCommandService
{
    public async Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(User.NormalizeEmail(command.Email), cancellationToken);
        if (user == null || !hashingService.VerifyPassword(command.Password, user.PasswordHash))
            return Result<(User, string)>.Failure(IamError.InvalidCredentials, localizer[nameof(IamError.InvalidCredentials)]);

        return Result<(User, string)>.Success((user, tokenService.GenerateToken(user)));
    }

    /// <summary>
    ///     One transaction: restaurant → owner (Administrator) → owner link →
    ///     free subscription (Subscription &amp; Billing ACL). A failure in any step
    ///     rolls back all of them.
    /// </summary>
    public async Task<Result<(User user, string token)>> Handle(SignUpCommand command, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(command.Email);
        if (await userRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken))
            return Result<(User, string)>.Failure(IamError.EmailAlreadyTaken, localizer[nameof(IamError.EmailAlreadyTaken), email]);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var restaurant = new Restaurant(command.RestaurantName.Trim());
            await restaurantRepository.AddAsync(restaurant, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            var user = new User(command.FullName, email, hashingService.HashPassword(command.Password), restaurant.Id, UserRole.Admin);
            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            restaurant.LinkOwner(user.Id);
            await unitOfWork.CompleteAsync(cancellationToken);

            await subscriptionContextFacade.CreateDefaultSubscription(restaurant.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<(User, string)>.Success((user, tokenService.GenerateToken(user)));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<(User, string)>.Failure(IamError.DatabaseError, localizer[nameof(IamError.DatabaseError)]);
        }
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

    /// <summary>The current password is verified before saving the new one.</summary>
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
}
