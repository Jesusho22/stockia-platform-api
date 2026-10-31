namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

public record PlanResource(int Id, string Name, decimal MonthlyPrice, IReadOnlyList<string> Features, bool Highlighted);
