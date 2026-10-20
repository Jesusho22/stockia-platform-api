namespace DataBit.StockIA.Platform.Subscription.Interfaces.Acl;

/// <summary>What other bounded contexts may ask Subscription &amp; Billing.</summary>
public interface ISubscriptionContextFacade
{
    /// <summary>Gives a new restaurant the free plan (IAM sign-up). Throws when there is no free plan.</summary>
    Task<int> CreateDefaultSubscription(int restaurantId, CancellationToken cancellationToken);
}
