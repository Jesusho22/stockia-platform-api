using DataBit.StockIA.Platform.RestaurantRegistration.Application.Services;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Queries;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Repositories;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.QueryServices;

public class RestaurantQueryService(IRestaurantRepository restaurantRepository) : IRestaurantQueryService
{
    public Task<Restaurant?> Handle(GetRestaurantByIdQuery query, CancellationToken cancellationToken) =>
        restaurantRepository.FindByIdAsync(query.RestaurantId, cancellationToken);
}
