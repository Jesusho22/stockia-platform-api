namespace DataBit.StockIA.Platform.Subscription.Domain.Model.ValueObjects;

/// <summary>Payment provider of the checkout (simulated in this version).</summary>
public enum PaymentMethod
{
    None,
    Stripe,
    Paypal
}
