using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.AnalyticsDashboard.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.AnalyticsDashboard.Interfaces.REST.Transform;

public static class DashboardSummaryResourceFromValueAssembler
{
    public static DashboardSummaryResource ToResource(DashboardSummary summary) => new(
        summary.RegisteredSupplies, summary.LowOrCriticalSupplies, summary.ExpiredSupplies, summary.ExpiringSoonSupplies,
        summary.InventoryValue, summary.ConfirmedSales, summary.Revenue, summary.RevenueToday, summary.PendingAlerts,
        summary.LatestForecastId, summary.ProjectedUnitsNext7Days, summary.ForecastConfidence);
}

public static class WeeklySavingsResourceFromValueAssembler
{
    private static SavingsLineResource ToLine(SavingsLine line) => new(line.ItemName, line.Unit, line.Quantity, line.Value);

    public static WeeklySavingsResource ToResource(WeeklySavings savings) => new(
        savings.From.ToString("yyyy-MM-dd"), savings.To.ToString("yyyy-MM-dd"), savings.EstimatedSavings, savings.RescuedValue,
        savings.PreviousRescuedValue, savings.WasteValue, savings.PreviousWasteValue, savings.WasteReduction, savings.ConfirmedSales,
        savings.PortionsSold, savings.Revenue, savings.PreviousRevenue, savings.ProjectedUnitsNextWeek, savings.ForecastConfidence,
        savings.TopRescued.Select(ToLine).ToList(), savings.Wasted.Select(ToLine).ToList());
}