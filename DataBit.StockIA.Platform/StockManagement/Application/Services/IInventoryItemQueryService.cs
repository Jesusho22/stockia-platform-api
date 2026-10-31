using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.StockManagement.Application.Services;

public interface IInventoryItemQueryService
{
    Task<IReadOnlyList<InventoryItem>> Handle(GetInventoryItemsByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<InventoryItem?> Handle(GetInventoryItemByIdQuery query, CancellationToken cancellationToken);
}
