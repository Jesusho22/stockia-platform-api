namespace DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Resources;

/// <summary>RenewalDate as yyyy-MM-dd; status and payment method as UPPER_SNAKE codes.</summary>
public record SubscriptionResource(int Id, int PlanId, string Status, string RenewalDate, string PaymentMethod);
