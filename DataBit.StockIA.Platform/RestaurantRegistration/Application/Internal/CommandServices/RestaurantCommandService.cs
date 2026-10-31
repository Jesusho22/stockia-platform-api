using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Errors;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Services;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Commands;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Repositories;
using DataBit.StockIA.Platform.RestaurantRegistration.Resources;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.CommandServices;

/// <summary>
///     Restaurant Onboarding (C4) and Profile Management of the restaurant.
///     StockIA serves one restaurant per account.
/// </summary>
public class RestaurantCommandService(
    IRestaurantRepository restaurantRepository,
    ExternalIamService externalIamService,
    ExternalSubscriptionService externalSubscriptionService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<RestaurantRegistrationMessages> localizer) : IRestaurantCommandService
{
    /// <summary>
    ///     One transaction: restaurant → owner account (IAM) → owner link → free
    ///     plan (Subscription &amp; Payments). A failure in any step rolls back all
    ///     of them; a duplicated email is rejected before writing anything.
    /// </summary>
    public async Task<Result<RestaurantRegistrationResult>> Handle(RegisterRestaurantCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        if (await externalIamService.EmailExists(email, cancellationToken)) return EmailTaken(email);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var restaurant = new Restaurant(command.RestaurantName.Trim());
            await restaurantRepository.AddAsync(restaurant, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            var owner = await externalIamService.CreateOwnerAccount(command.OwnerFullName, email, command.Password, restaurant.Id,
                cancellationToken);
            if (owner == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return EmailTaken(email);
            }

            restaurant.LinkOwner(owner.UserId);
            await unitOfWork.CompleteAsync(cancellationToken);

            await externalSubscriptionService.StartFreePlan(restaurant.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<RestaurantRegistrationResult>.Success(new RestaurantRegistrationResult(restaurant, owner));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<RestaurantRegistrationResult>.Failure(RestaurantRegistrationError.DatabaseError,
                localizer[nameof(RestaurantRegistrationError.DatabaseError)]);
        }
    }

    /// <summary>Restaurant data (Account settings). Only the Administrator reaches it (checked by the controller).</summary>
    public async Task<Result<Restaurant>> Handle(UpdateRestaurantCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await restaurantRepository.FindByIdAsync(command.RestaurantId, cancellationToken);
        if (restaurant == null)
            return Result<Restaurant>.Failure(RestaurantRegistrationError.RestaurantNotFound,
                localizer[nameof(RestaurantRegistrationError.RestaurantNotFound)]);

        restaurant.UpdateProfile(command.Name, command.Address, command.Phone);
        restaurantRepository.Update(restaurant);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Restaurant>.Success(restaurant);
    }

    private Result<RestaurantRegistrationResult> EmailTaken(string email) =>
        Result<RestaurantRegistrationResult>.Failure(RestaurantRegistrationError.EmailAlreadyTaken,
            localizer[nameof(RestaurantRegistrationError.EmailAlreadyTaken), email]);
}
