using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.OutboundServices;

/// <summary>
///     Sends alerts by WhatsApp or email and transactional emails. The current
///     implementation is simulated (it logs the message); Twilio (WhatsApp) and
///     SendGrid (C4: SendGrid Adapter) only need to implement this interface.
/// </summary>
public interface INotificationSender
{
    /// <summary>True when nothing really leaves the server.</summary>
    bool IsSimulated { get; }

    Task<bool> SendAsync(int restaurantId, AlertChannel channel, string message, CancellationToken cancellationToken);

    Task<bool> SendEmailAsync(int restaurantId, string to, string subject, string body, CancellationToken cancellationToken);
}
