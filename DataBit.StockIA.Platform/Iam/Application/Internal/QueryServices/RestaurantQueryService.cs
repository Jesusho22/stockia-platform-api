using DataBit.StockIA.Platform.Iam.Application.QueryServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.QueryServices;

public class RestaurantQueryService(IRestaurantRepository restaurantRepository) : IRestaurantQueryService
{
    public Task<Restaurant?> Handle(GetRestaurantByIdQuery query, CancellationToken cancellationToken) =>
        restaurantRepository.FindByIdAsync(query.RestaurantId, cancellationToken);
}
