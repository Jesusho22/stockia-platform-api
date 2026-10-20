using DataBit.StockIA.Platform.Subscription.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;

/// <summary>
///     The plan a restaurant is subscribed to ("Subscription" in the Web
///     Application). Named RestaurantSubscription so it does not clash with the
///     bounded context namespace.
/// </summary>
public class RestaurantSubscription(int restaurantId, int planId) : IAuditableEntity
{
    /// <summary>Days covered by one monthly payment.</summary>
    public const int BillingPeriodDays = 30;

    public RestaurantSubscription() : this(0, 0)
    {
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; } = restaurantId;
    public int PlanId { get; private set; } = planId;
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.Active;
    public DateOnly RenewalDate { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; } = PaymentMethod.None;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Activates a plan paid today; it renews after one billing period.</summary>
    public RestaurantSubscription Activate(int planId, PaymentMethod paymentMethod, DateOnly today)
    {
        PlanId = planId;
        PaymentMethod = paymentMethod;
        Status = SubscriptionStatus.Active;
        RenewalDate = today.AddDays(BillingPeriodDays);
        return this;
    }
}
