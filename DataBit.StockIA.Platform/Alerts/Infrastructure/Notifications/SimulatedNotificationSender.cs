using DataBit.StockIA.Platform.Alerts.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Alerts.Infrastructure.Notifications;

/// <summary>Simulated WhatsApp / email delivery: always succeeds and logs the message.</summary>
public class SimulatedNotificationSender(ILogger<SimulatedNotificationSender> logger) : INotificationSender
{
    public Task<bool> SendAsync(int restaurantId, AlertChannel channel, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Channel}] restaurant {RestaurantId}: {Message}", channel, restaurantId, message);
        return Task.FromResult(true);
    }
}
