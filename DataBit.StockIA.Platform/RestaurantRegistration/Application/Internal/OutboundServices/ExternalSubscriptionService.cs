using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.Internal.OutboundServices;

/// <summary>Plan selection (C4: Plan Selection Component): a new restaurant starts on the free plan.</summary>
public class ExternalSubscriptionService(ISubscriptionContextFacade subscriptionContextFacade)
{
    public Task<int> StartFreePlan(int restaurantId, CancellationToken cancellationToken) =>
        subscriptionContextFacade.CreateDefaultSubscription(restaurantId, cancellationToken);
}
