namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Errors;

/// <summary>Errors of the Notifications &amp; Messaging bounded context (their names are the problem titles).</summary>
public enum NotificationsMessagingError
{
    AlertNotFound,
    NotDelivered,
    AlreadyDelivered,
    DeliveryFailed
}
