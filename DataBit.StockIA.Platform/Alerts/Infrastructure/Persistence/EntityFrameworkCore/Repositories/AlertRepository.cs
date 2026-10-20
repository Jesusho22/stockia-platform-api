using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Alerts.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class AlertRepository(AppDbContext context) : BaseRepository<Alert>(context), IAlertRepository
{
    public async Task<IReadOnlyList<Alert>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await Context.Set<Alert>().Where(alert => alert.RestaurantId == restaurantId)
            .OrderByDescending(alert => alert.CreatedAt).ThenByDescending(alert => alert.Id).ToListAsync(cancellationToken);

    public async Task<Alert?> FindOpenAsync(int restaurantId, int inventoryItemId, AlertType type, CancellationToken cancellationToken = default)
    {
        // Alerts added in this same unit of work are not in the database yet.
        var local = Context.Set<Alert>().Local.FirstOrDefault(alert => alert.RestaurantId == restaurantId &&
            alert.InventoryItemId == inventoryItemId && alert.Type == type && !alert.Acknowledged);
        return local ?? await Context.Set<Alert>().FirstOrDefaultAsync(alert => alert.RestaurantId == restaurantId &&
            alert.InventoryItemId == inventoryItemId && alert.Type == type && !alert.Acknowledged, cancellationToken);
    }
}
