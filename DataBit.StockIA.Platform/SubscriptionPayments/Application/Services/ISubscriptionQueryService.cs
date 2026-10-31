using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;

public interface ISubscriptionQueryService
{
    Task<IEnumerable<Plan>> Handle(GetAllPlansQuery query, CancellationToken cancellationToken);
    Task<RestaurantSubscription?> Handle(GetSubscriptionByRestaurantIdQuery query, CancellationToken cancellationToken);
}
