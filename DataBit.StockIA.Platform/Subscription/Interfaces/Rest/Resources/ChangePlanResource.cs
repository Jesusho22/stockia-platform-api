using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Resources;

/// <summary>PaymentMethod: STRIPE or PAYPAL (NONE only for the free plan).</summary>
public record ChangePlanResource([Range(1, int.MaxValue)] int PlanId, [Required] string PaymentMethod);
