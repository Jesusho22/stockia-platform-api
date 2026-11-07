namespace DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;

public record ConsumptionEntry(DateOnly ConsumedOn, string ItemName, string Unit, decimal Quantity, decimal Value, int DaysBeforeExpiration);

public record LotSnapshot(string Name, string Unit, decimal Quantity, DateOnly ExpirationDate, decimal StockValue);

public record SalesTotals(int ConfirmedSales, int PortionsSold, decimal Revenue);

public record SavingsLine(string ItemName, string Unit, decimal Quantity, decimal Value);

public record WeeklySavings(
    DateOnly From,
    DateOnly To,
    decimal EstimatedSavings,
    decimal RescuedValue,
    decimal PreviousRescuedValue,
    decimal WasteValue,
    decimal PreviousWasteValue,
    decimal WasteReduction,
    int ConfirmedSales,
    int PortionsSold,
    decimal Revenue,
    decimal PreviousRevenue,
    int ProjectedUnitsNextWeek,
    decimal? ForecastConfidence,
    IReadOnlyList<SavingsLine> TopRescued,
    IReadOnlyList<SavingsLine> Wasted);