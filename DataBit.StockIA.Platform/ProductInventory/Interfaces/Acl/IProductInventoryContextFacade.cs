namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Acl;

/// <summary>A supply that blocks a sale: reason NOT_FOUND, EXPIRED or INSUFFICIENT; quantities as exact text.</summary>
public record ShortageInfo(string ItemName, string Unit, string Reason, string Required, string Available, string Expired);

/// <summary>Result of checking whether a dish can be sold right now.</summary>
public record RecipeSaleCheck(int RecipeId, string DishName, decimal UnitPrice, bool CanFulfill, IReadOnlyList<ShortageInfo> Shortages);

/// <summary>ProductKey (name|unit) groups the lots of the same product.</summary>
public record RecipeIngredientInfo(int InventoryItemId, string ItemName, string Unit, decimal QuantityRequired, string ProductKey);

public record RecipeInfo(int RecipeId, string DishName, decimal Price, IReadOnlyList<RecipeIngredientInfo> Ingredients);

/// <summary>A supply lot as other contexts see it. Status: AVAILABLE, LOW, CRITICAL or EXPIRED.</summary>
public record InventoryLotInfo(
    int InventoryItemId, string Name, string Unit, decimal Quantity, decimal MinThreshold, DateOnly ExpirationDate,
    int DaysToExpire, decimal UnitCost, decimal StockValue, string Status, string ProductKey);

/// <summary>
///     The only entry point of other bounded contexts into Inventory &amp;
///     Recipe Management (never its repositories or tables).
/// </summary>
public interface IProductInventoryContextFacade
{
    /// <summary>Checks stock (expired lots excluded) and price of a dish — Sales &amp; Order Management.</summary>
    Task<RecipeSaleCheck?> CheckRecipeSale(int restaurantId, int recipeId, int portions, CancellationToken cancellationToken);

    /// <summary>Discounts the supplies of a sold dish. Returns the shortages when it could not.</summary>
    Task<IReadOnlyList<ShortageInfo>> DeductStockForRecipe(int restaurantId, int recipeId, int portions, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipeInfo>> GetActiveRecipes(int restaurantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryLotInfo>> GetInventory(int restaurantId, CancellationToken cancellationToken);

    Task<InventoryLotInfo?> GetInventoryLot(int restaurantId, int inventoryItemId, CancellationToken cancellationToken);
}
