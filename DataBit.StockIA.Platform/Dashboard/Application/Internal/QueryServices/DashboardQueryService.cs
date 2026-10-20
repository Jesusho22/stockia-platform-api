using DataBit.StockIA.Platform.Dashboard.Application.QueryServices;
using DataBit.StockIA.Platform.Dashboard.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Alerts.Interfaces.Acl;
using DataBit.StockIA.Platform.DemandForecasting.Interfaces.Acl;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Acl;
using DataBit.StockIA.Platform.SalesOrder.Interfaces.Acl;

namespace DataBit.StockIA.Platform.Dashboard.Application.Internal.QueryServices;

/// <summary>
///     Dashboard &amp; Analytics has no aggregates of its own: it composes the
///     other bounded contexts through their ACL facades (same as QULLQA).
/// </summary>
public class DashboardQueryService(
    IProductInventoryContextFacade productInventoryContextFacade,
    ISalesOrderContextFacade salesOrderContextFacade,
    IAlertsContextFacade alertsContextFacade,
    IDemandForecastingContextFacade demandForecastingContextFacade) : IDashboardQueryService
{
    public async Task<DashboardSummaryResource> GetSummary(int restaurantId, CancellationToken cancellationToken)
    {
        var inventory = await productInventoryContextFacade.GetInventory(restaurantId, cancellationToken);
        var sales = await salesOrderContextFacade.GetSummary(restaurantId, cancellationToken);
        var pendingAlerts = await alertsContextFacade.CountPendingAlerts(restaurantId, cancellationToken);
        var forecast = await demandForecastingContextFacade.GetLatestForecast(restaurantId, cancellationToken);
        return new DashboardSummaryResource(
            inventory.Count,
            inventory.Count(lot => lot.Status is "LOW" or "CRITICAL"),
            inventory.Count(lot => lot.Status == "EXPIRED"),
            inventory.Count(lot => lot.Status != "EXPIRED" && lot.DaysToExpire is >= 0 and <= 3),
            inventory.Sum(lot => lot.StockValue),
            sales.ConfirmedSales,
            sales.Revenue,
            sales.RevenueToday,
            pendingAlerts,
            forecast?.ForecastId,
            forecast?.Dishes.Sum(dish => dish.TotalUnits) ?? 0,
            forecast?.ConfidenceScore);
    }
}
