namespace DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;

/// <summary>KPIs of the business dashboard; inventory value and revenue in soles.</summary>
public record DashboardSummary(
    int RegisteredSupplies, int LowOrCriticalSupplies, int ExpiredSupplies, int ExpiringSoonSupplies, decimal InventoryValue,
    int ConfirmedSales, decimal Revenue, decimal RevenueToday, int PendingAlerts, int? LatestForecastId, int ProjectedUnitsNext7Days,
    decimal? ForecastConfidence);
