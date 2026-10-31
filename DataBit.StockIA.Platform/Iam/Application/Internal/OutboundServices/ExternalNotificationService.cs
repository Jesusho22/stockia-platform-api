using DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.ACL;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;

/// <summary>Anti-corruption layer towards Notifications &amp; Messaging (the recovery email).</summary>
public class ExternalNotificationService(INotificationsMessagingContextFacade notificationsMessagingContextFacade)
{
    public Task<bool> SendEmail(int restaurantId, string to, string subject, string body, CancellationToken cancellationToken) =>
        notificationsMessagingContextFacade.SendEmail(restaurantId, to, subject, body, cancellationToken);

    /// <summary>True while the email provider is simulated (nothing really leaves the server).</summary>
    public bool IsSimulated => notificationsMessagingContextFacade.IsEmailSimulated;
}
