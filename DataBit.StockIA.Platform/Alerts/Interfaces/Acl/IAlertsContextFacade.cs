namespace DataBit.StockIA.Platform.Alerts.Interfaces.Acl;

public interface IAlertsContextFacade
{
    Task<int> CountPendingAlerts(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Creates the automatic alerts of every supply of a restaurant (used after seeding demo data).</summary>
    Task EvaluateInventoryAlerts(int restaurantId, CancellationToken cancellationToken);
}
