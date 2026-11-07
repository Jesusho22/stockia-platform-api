using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Queries;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.QueryServices;

public class SubscriptionQueryService(IPlanRepository planRepository, ISubscriptionRepository subscriptionRepository)
    : ISubscriptionQueryService
{
    public async Task<IEnumerable<Plan>> Handle(GetAllPlansQuery query, CancellationToken cancellationToken) =>
        (await planRepository.ListAsync(cancellationToken)).OrderBy(plan => plan.MonthlyPrice);

    public Task<RestaurantSubscription?> Handle(GetSubscriptionByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        subscriptionRepository.FindByRestaurantIdAsync(query.RestaurantId, cancellationToken);
}