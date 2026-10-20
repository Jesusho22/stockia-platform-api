using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.Subscription.Application.QueryServices;

public interface ISubscriptionQueryService
{
    Task<IEnumerable<Plan>> Handle(GetAllPlansQuery query, CancellationToken cancellationToken);
    Task<RestaurantSubscription?> Handle(GetSubscriptionByRestaurantIdQuery query, CancellationToken cancellationToken);
}
