using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Internal.OutboundServices;

/// <summary>
///     Anti-corruption layer towards Stock Management: translates a recipe
///     into the ingredient requirements the inventory understands.
/// </summary>
public class ExternalStockService(IStockManagementContextFacade stockManagementContextFacade)
{
    public static IReadOnlyList<IngredientRequirement> RequirementsOf(Recipe recipe) =>
        recipe.Ingredients.Select(line => new IngredientRequirement(line.InventoryItemId, line.InventoryItemName, line.Unit,
            line.QuantityRequired)).ToList();

    public Task<StockAvailability> CheckAvailability(Recipe recipe, int portions, CancellationToken cancellationToken) =>
        stockManagementContextFacade.CheckAvailability(recipe.RestaurantId, RequirementsOf(recipe), portions, cancellationToken);

    public Task<IReadOnlyList<ShortageInfo>> DeductStock(Recipe recipe, int portions, CancellationToken cancellationToken) =>
        stockManagementContextFacade.DeductStock(recipe.RestaurantId, RequirementsOf(recipe), portions, cancellationToken);

    /// <summary>Price of one portion of each recipe, in the same order.</summary>
    public Task<IReadOnlyList<decimal>> PricesOf(int restaurantId, IReadOnlyList<Recipe> recipes, CancellationToken cancellationToken) =>
        stockManagementContextFacade.PricesOf(restaurantId, recipes.Select(RequirementsOf).ToList(), cancellationToken);

    public Task<IReadOnlyList<InventoryLotInfo>> GetInventory(int restaurantId, CancellationToken cancellationToken) =>
        stockManagementContextFacade.GetInventory(restaurantId, cancellationToken);
}
