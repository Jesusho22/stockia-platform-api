namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>Payment provider of the checkout (simulated in this version).</summary>
public enum PaymentMethod
{
    None,
    Stripe,
    Paypal
}