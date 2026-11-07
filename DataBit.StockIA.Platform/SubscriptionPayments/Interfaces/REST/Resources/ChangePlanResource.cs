using System.ComponentModel.DataAnnotations;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

/// <summary>PaymentMethod: STRIPE or PAYPAL (NONE only for the free plan).</summary>
public record ChangePlanResource([Range(1, int.MaxValue)] int PlanId, [Required] string PaymentMethod);