namespace DataBit.StockIA.Platform.AnalyticsDashboard.Interfaces.REST.Resources;

public record DashboardSummaryResource(int RegisteredSupplies, int LowOrCriticalSupplies, int ExpiredSupplies, int ExpiringSoonSupplies, decimal InventoryValue,
    int ConfirmedSales, decimal Revenue, decimal RevenueToday, int PendingAlerts, int? LatestForecastId, int ProjectedUnitsNext7Days,
    decimal? ForecastConfidence);