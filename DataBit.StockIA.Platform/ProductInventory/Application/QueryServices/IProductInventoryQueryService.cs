using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.ProductInventory.Application.QueryServices;

public interface IProductInventoryQueryService
{
    Task<IReadOnlyList<InventoryItem>> Handle(GetInventoryItemsByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<InventoryItem?> Handle(GetInventoryItemByIdQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Recipe>> Handle(GetRecipesByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<Recipe?> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken);
}
