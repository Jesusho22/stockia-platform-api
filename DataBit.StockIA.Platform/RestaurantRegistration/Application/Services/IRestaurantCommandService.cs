using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Patterns;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.Services;

public interface IRestaurantCommandService
{
    /// <summary>Creates the restaurant, its owner (Administrator) and the free subscription, and signs the owner in.</summary>
    Task<Result<RestaurantRegistrationResult>> Handle(RegisterRestaurantCommand command, CancellationToken cancellationToken);

    Task<Result<Restaurant>> Handle(UpdateRestaurantCommand command, CancellationToken cancellationToken);
}
