namespace DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.ValueObjects;

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