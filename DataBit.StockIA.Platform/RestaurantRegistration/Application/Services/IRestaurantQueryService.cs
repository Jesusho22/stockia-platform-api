using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.Services;

public interface IRestaurantQueryService
{
    Task<Restaurant?> Handle(GetRestaurantByIdQuery query, CancellationToken cancellationToken);
}
