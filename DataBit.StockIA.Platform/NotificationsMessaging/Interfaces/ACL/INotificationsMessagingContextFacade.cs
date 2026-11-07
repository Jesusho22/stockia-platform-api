namespace DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.ACL;

public interface INotificationsMessagingContextFacade
{
    Task<int> CountPendingAlerts(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Creates the automatic alerts of every supply of a restaurant (used after seeding demo data).</summary>
    Task EvaluateInventoryAlerts(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Sends a transactional email (e.g. the password recovery link of IAM).</summary>
    Task<bool> SendEmail(int restaurantId, string to, string subject, string body, CancellationToken cancellationToken);

    /// <summary>True while the email provider is simulated.</summary>
    bool IsEmailSimulated { get; }
}