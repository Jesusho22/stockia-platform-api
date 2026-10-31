using DataBit.StockIA.Platform.StockManagement.Application.Services;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Queries;
using DataBit.StockIA.Platform.StockManagement.Domain.Repositories;

namespace DataBit.StockIA.Platform.StockManagement.Application.Internal.QueryServices;

public class InventoryItemQueryService(IInventoryItemRepository inventoryItemRepository) : IInventoryItemQueryService
{
    public Task<IReadOnlyList<InventoryItem>> Handle(GetInventoryItemsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        inventoryItemRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);

    public async Task<InventoryItem?> Handle(GetInventoryItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await inventoryItemRepository.FindByIdAsync(query.InventoryItemId, cancellationToken);
        return item?.RestaurantId == query.RestaurantId ? item : null;
    }
}
