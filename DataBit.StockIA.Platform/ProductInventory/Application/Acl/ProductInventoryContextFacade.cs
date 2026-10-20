using DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;
using DataBit.StockIA.Platform.ProductInventory.Domain.Services;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Acl;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Application.Acl;

public class ProductInventoryContextFacade(
    IInventoryItemRepository inventoryItemRepository,
    IRecipeRepository recipeRepository,
    IInventoryItemCommandService inventoryItemCommandService,
    IRestaurantClock clock) : IProductInventoryContextFacade
{
    public async Task<RecipeSaleCheck?> CheckRecipeSale(int restaurantId, int recipeId, int portions, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(recipeId, cancellationToken);
        if (recipe == null || recipe.RestaurantId != restaurantId) return null;
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        var plan = StockAllocationService.Plan(recipe, items, portions, clock.Today);
        return new RecipeSaleCheck(recipe.Id, recipe.DishName, StockAllocationService.RecipePrice(recipe, items, clock.Today),
            plan.CanFulfill, plan.Shortages.Select(ToShortageInfo).ToList());
    }

    public async Task<IReadOnlyList<ShortageInfo>> DeductStockForRecipe(int restaurantId, int recipeId, int portions,
        CancellationToken cancellationToken)
    {
        var result = await inventoryItemCommandService.Handle(new DeductStockForRecipeCommand(restaurantId, recipeId, portions),
            cancellationToken);
        if (result.IsSuccess) return [];
        return result.Details is IEnumerable<Shortage> shortages
            ? shortages.Select(ToShortageInfo).ToList()
            : [new ShortageInfo(string.Empty, string.Empty, "NOT_FOUND", "0", "0", "0")];
    }

    public async Task<IReadOnlyList<RecipeInfo>> GetActiveRecipes(int restaurantId, CancellationToken cancellationToken)
    {
        var recipes = await recipeRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        return recipes.Where(recipe => recipe.Active).Select(recipe => new RecipeInfo(recipe.Id, recipe.DishName,
            StockAllocationService.RecipePrice(recipe, items, clock.Today),
            recipe.Ingredients.Select(line => new RecipeIngredientInfo(line.InventoryItemId, line.InventoryItemName, line.Unit,
                line.QuantityRequired.ToDecimal(),
                items.FirstOrDefault(item => item.Id == line.InventoryItemId)?.ProductKey
                ?? InventoryItem.ProductKeyOf(line.InventoryItemName, line.Unit))).ToList())).ToList();
    }

    public async Task<IReadOnlyList<InventoryLotInfo>> GetInventory(int restaurantId, CancellationToken cancellationToken)
    {
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        return items.Select(ToLotInfo).ToList();
    }

    public async Task<InventoryLotInfo?> GetInventoryLot(int restaurantId, int inventoryItemId, CancellationToken cancellationToken)
    {
        var item = await inventoryItemRepository.FindByIdAsync(inventoryItemId, cancellationToken);
        return item == null || item.RestaurantId != restaurantId ? null : ToLotInfo(item);
    }

    private InventoryLotInfo ToLotInfo(InventoryItem item) => new(item.Id, item.Name, item.Unit, item.Quantity.ToDecimal(),
        item.MinThreshold.ToDecimal(), item.ExpirationDate, item.DaysToExpire(clock.Today), item.UnitCost, item.StockValue,
        item.StatusOn(clock.Today).ToCode(), item.ProductKey);

    private static ShortageInfo ToShortageInfo(Shortage shortage) => new(shortage.ItemName, shortage.Unit, shortage.Reason.ToCode(),
        shortage.Required.ToString(), shortage.Available.ToString(), shortage.Expired.ToString());
}
