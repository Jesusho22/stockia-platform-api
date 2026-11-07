using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.OutboundServices;

public interface INotificationSender
{
    bool IsSimulated { get; }

    Task<bool> SendAsync(int restaurantId, AlertChannel channel, string message, CancellationToken cancellationToken);

    Task<bool> SendEmailAsync(int restaurantId, string to, string subject, string body, CancellationToken cancellationToken);
}