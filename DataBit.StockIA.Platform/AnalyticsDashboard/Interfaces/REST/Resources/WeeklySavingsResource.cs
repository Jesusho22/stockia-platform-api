using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
namespace DataBit.StockIA.Platform.AnalyticsDashboard.Interfaces.REST.Resources;

public record SavingsLineResource(string ItemName, string Unit, decimal Quantity, decimal Value);

public record WeeklySavingsResource(
    string From,
    string To,
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
    IReadOnlyList<SavingsLineResource> TopRescued,
    IReadOnlyList<SavingsLineResource> Wasted);