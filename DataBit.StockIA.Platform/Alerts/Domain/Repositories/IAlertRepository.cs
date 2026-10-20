using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.Alerts.Domain.Repositories;

public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<IReadOnlyList<Alert>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>Unattended automatic alert of a supply and type (to refresh it instead of repeating it).</summary>
    Task<Alert?> FindOpenAsync(int restaurantId, int inventoryItemId, AlertType type, CancellationToken cancellationToken = default);
}
