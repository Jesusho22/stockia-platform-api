using DataBit.StockIA.Platform.ProductInventory.Application.QueryServices;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Queries;
using DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;

namespace DataBit.StockIA.Platform.ProductInventory.Application.Internal.QueryServices;

public class ProductInventoryQueryService(IInventoryItemRepository inventoryItemRepository, IRecipeRepository recipeRepository)
    : IProductInventoryQueryService
{
    public Task<IReadOnlyList<InventoryItem>> Handle(GetInventoryItemsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        inventoryItemRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);

    public async Task<InventoryItem?> Handle(GetInventoryItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await inventoryItemRepository.FindByIdAsync(query.InventoryItemId, cancellationToken);
        return item?.RestaurantId == query.RestaurantId ? item : null;
    }

    public Task<IReadOnlyList<Recipe>> Handle(GetRecipesByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        recipeRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);

    public async Task<Recipe?> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(query.RecipeId, cancellationToken);
        return recipe?.RestaurantId == query.RestaurantId ? recipe : null;
    }
}
