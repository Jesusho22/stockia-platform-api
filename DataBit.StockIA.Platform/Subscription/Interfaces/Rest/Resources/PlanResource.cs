namespace DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Resources;

public record PlanResource(int Id, string Name, decimal MonthlyPrice, IReadOnlyList<string> Features, bool Highlighted);
