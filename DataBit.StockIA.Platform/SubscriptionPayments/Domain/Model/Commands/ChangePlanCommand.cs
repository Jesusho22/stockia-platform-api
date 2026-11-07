using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;

public record ChangePlanCommand(int RestaurantId, int PlanId, PaymentMethod PaymentMethod);