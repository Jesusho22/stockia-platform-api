using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.StockManagement.Domain.Repositories;

public interface IInventoryItemRepository : IBaseRepository<InventoryItem>
{
    /// <summary>Newest first: a new supply appears at the top of the list.</summary>
    Task<IReadOnlyList<InventoryItem>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
