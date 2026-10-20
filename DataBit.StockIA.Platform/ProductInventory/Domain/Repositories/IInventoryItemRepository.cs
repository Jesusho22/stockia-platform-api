using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;

public interface IInventoryItemRepository : IBaseRepository<InventoryItem>
{
    /// <summary>Newest first: a new supply appears at the top of the list.</summary>
    Task<IReadOnlyList<InventoryItem>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
