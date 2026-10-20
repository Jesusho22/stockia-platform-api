namespace DataBit.StockIA.Platform.Dashboard.Interfaces.Rest.Resources;

/// <summary>KPIs of the business dashboard; inventoryValue and revenue in soles.</summary>
public record DashboardSummaryResource(
    int RegisteredSupplies, int LowOrCriticalSupplies, int ExpiredSupplies, int ExpiringSoonSupplies, decimal InventoryValue,
    int ConfirmedSales, decimal Revenue, decimal RevenueToday, int PendingAlerts, int? LatestForecastId, int ProjectedUnitsNext7Days,
    decimal? ForecastConfidence);
