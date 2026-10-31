using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;

/// <summary>Stock a sale used from a lot (input of the Weekly Savings Calculator).</summary>
public record ConsumptionEntry(DateOnly ConsumedOn, string ItemName, string Unit, decimal Quantity, decimal Value, int DaysBeforeExpiration);

/// <summary>A supply lot as it is now (input of the Weekly Savings Calculator).</summary>
public record LotSnapshot(string Name, string Unit, decimal Quantity, DateOnly ExpirationDate, decimal StockValue);

/// <summary>Sales of a period (input of the Weekly Savings Calculator).</summary>
public record SalesTotals(int ConfirmedSales, int PortionsSold, decimal Revenue);

/// <summary>A supply in the savings detail: what was used before it expired, or what expired.</summary>
public record SavingsLine(string ItemName, string Unit, decimal Quantity, decimal Value);

/// <summary>
///     Weekly savings summary (C4: Weekly Savings Calculator). All amounts in
///     soles. "Rescued" is stock used by sales during its last days before
///     expiring; "waste" is stock that expired without being used.
/// </summary>
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
