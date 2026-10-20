using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Iam.Application.CommandServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.Errors;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Iam.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.CommandServices;

/// <summary>
///     Restaurant data (Account settings › Restaurant data). Only the
///     Administrator reaches it (checked by the controller).
/// </summary>
public class RestaurantCommandService(
    IRestaurantRepository restaurantRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<IamMessages> localizer) : IRestaurantCommandService
{
    public async Task<Result<Restaurant>> Handle(UpdateRestaurantCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await restaurantRepository.FindByIdAsync(command.RestaurantId, cancellationToken);
        if (restaurant == null)
            return Result<Restaurant>.Failure(IamError.RestaurantNotFound, localizer[nameof(IamError.RestaurantNotFound)]);

        restaurant.UpdateProfile(command.Name, command.Address, command.Phone);
        restaurantRepository.Update(restaurant);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Restaurant>.Success(restaurant);
    }
}
