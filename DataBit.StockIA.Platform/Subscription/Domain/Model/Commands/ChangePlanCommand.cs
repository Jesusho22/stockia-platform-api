using DataBit.StockIA.Platform.Subscription.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Subscription.Domain.Model.Commands;

public record ChangePlanCommand(int RestaurantId, int PlanId, PaymentMethod PaymentMethod);
