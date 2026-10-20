using DataBit.StockIA.Platform.Subscription.Application.QueryServices;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Queries;
using DataBit.StockIA.Platform.Subscription.Domain.Repositories;

namespace DataBit.StockIA.Platform.Subscription.Application.Internal.QueryServices;

public class SubscriptionQueryService(IPlanRepository planRepository, ISubscriptionRepository subscriptionRepository)
    : ISubscriptionQueryService
{
    public async Task<IEnumerable<Plan>> Handle(GetAllPlansQuery query, CancellationToken cancellationToken) =>
        (await planRepository.ListAsync(cancellationToken)).OrderBy(plan => plan.MonthlyPrice);

    public Task<RestaurantSubscription?> Handle(GetSubscriptionByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        subscriptionRepository.FindByRestaurantIdAsync(query.RestaurantId, cancellationToken);
}
