using DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Infrastructure.Notifications;

public class SimulatedNotificationSender(ILogger<SimulatedNotificationSender> logger) : INotificationSender
{
    public bool IsSimulated => true;

    public Task<bool> SendAsync(int restaurantId, AlertChannel channel, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Channel}] restaurant {RestaurantId}: {Message}", channel, restaurantId, message);
        return Task.FromResult(true);
    }

    public Task<bool> SendEmailAsync(int restaurantId, string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("[EMAIL] restaurant {RestaurantId} to {To}: {Subject}\n{Body}", restaurantId, to, subject, body);
        return Task.FromResult(true);
    }
}