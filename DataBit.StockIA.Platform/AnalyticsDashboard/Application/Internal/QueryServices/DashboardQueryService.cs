using DataBit.StockIA.Platform.AnalyticsDashboard.Application.Services;
using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.Queries;
using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Services;
using DataBit.StockIA.Platform.MlRecommendations.Interfaces.ACL;
using DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.ACL;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.ACL;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;

namespace DataBit.StockIA.Platform.AnalyticsDashboard.Application.Internal.QueryServices;

public class DashboardQueryService(
    IStockManagementContextFacade stockManagementContextFacade,
    IRecipesManagementContextFacade recipesManagementContextFacade,
    INotificationsMessagingContextFacade notificationsMessagingContextFacade,
    IMlRecommendationsContextFacade mlRecommendationsContextFacade,
    IRestaurantClock clock) : IDashboardQueryService
{
    public async Task<DashboardSummary> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var restaurantId = query.RestaurantId;
        var inventory = await stockManagementContextFacade.GetInventory(restaurantId, cancellationToken);
        var sales = await recipesManagementContextFacade.GetSummary(restaurantId, cancellationToken);
        var pendingAlerts = await notificationsMessagingContextFacade.CountPendingAlerts(restaurantId, cancellationToken);
        var forecast = await mlRecommendationsContextFacade.GetLatestForecast(restaurantId, cancellationToken);
        return new DashboardSummary(
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

    public async Task<WeeklySavings> Handle(GetWeeklySavingsQuery query, CancellationToken cancellationToken)
    {
        var restaurantId = query.RestaurantId;
        var today = clock.Today;
        var (from, to) = WeeklySavingsCalculator.WeekEndingOn(today);
        var previousFrom = from.AddDays(-WeeklySavingsCalculator.WeekDays);

        var consumptions = await stockManagementContextFacade.GetConsumptions(restaurantId, previousFrom, to, cancellationToken);
        var lots = await stockManagementContextFacade.GetInventory(restaurantId, cancellationToken);
        var current = await recipesManagementContextFacade.GetSalesBetween(restaurantId, from, to, cancellationToken);
        var previous = await recipesManagementContextFacade.GetSalesBetween(restaurantId, previousFrom, from.AddDays(-1), cancellationToken);
        var forecast = await mlRecommendationsContextFacade.GetLatestForecast(restaurantId, cancellationToken);

        return WeeklySavingsCalculator.Calculate(today,
            consumptions.Select(entry => new ConsumptionEntry(entry.ConsumedOn, entry.ItemName, entry.Unit, entry.Quantity, entry.Value,
                entry.DaysBeforeExpiration)).ToList(),
            lots.Select(lot => new LotSnapshot(lot.Name, lot.Unit, lot.Quantity, lot.ExpirationDate, lot.StockValue)).ToList(),
            new SalesTotals(current.ConfirmedSales, current.PortionsSold, current.Revenue),
            new SalesTotals(previous.ConfirmedSales, previous.PortionsSold, previous.Revenue),
            forecast?.Dishes.Sum(dish => dish.TotalUnits) ?? 0,
            forecast?.ConfidenceScore);
    }
}