namespace DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;

public enum AlertType
{
    LowStock,
    CriticalStock,
    ExpiringSoon,
    IotFault
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum AlertChannel
{
    Whatsapp,
    Email
}

public enum RecommendationType
{
    PurchaseSuggestion,
    MenuAdjustment
}
