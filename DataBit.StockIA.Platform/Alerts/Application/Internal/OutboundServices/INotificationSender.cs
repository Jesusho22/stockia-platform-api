using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Alerts.Application.Internal.OutboundServices;

/// <summary>
///     Sends an alert by WhatsApp or email. The current implementation is
///     simulated (logs the message); Twilio (WhatsApp) and SendGrid (email)
///     only need to implement this interface.
/// </summary>
public interface INotificationSender
{
    Task<bool> SendAsync(int restaurantId, AlertChannel channel, string message, CancellationToken cancellationToken);
}
