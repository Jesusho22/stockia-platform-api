namespace DataBit.StockIA.Platform.RecipesManagement.Interfaces.ACL;

/// <summary>ProductKey (name|unit) groups the lots of the same product.</summary>
public record RecipeIngredientInfo(int InventoryItemId, string ItemName, string Unit, decimal QuantityRequired, string ProductKey);

public record RecipeInfo(int RecipeId, string DishName, decimal Price, IReadOnlyList<RecipeIngredientInfo> Ingredients);

/// <summary>Units of a dish sold on a local calendar day (confirmed sales only).</summary>
public record DailyDishSales(DateOnly Date, string DishName, int Units);

public record SalesSummary(int ConfirmedSales, decimal Revenue, decimal RevenueToday);

/// <summary>Confirmed sales between two local dates (inclusive).</summary>
public record SalesPeriodSummary(DateOnly From, DateOnly To, int ConfirmedSales, int PortionsSold, decimal Revenue);

/// <summary>What other bounded contexts may read from Recipes Management.</summary>
public interface IRecipesManagementContextFacade
{
    /// <summary>Active dishes with their ingredients and price — ML &amp; Recommendations.</summary>
    Task<IReadOnlyList<RecipeInfo>> GetActiveRecipes(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Sales history for the demand forecast (days without sales are simply missing).</summary>
    Task<IReadOnlyList<DailyDishSales>> GetDailyUnitsByDish(int restaurantId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<SalesSummary> GetSummary(int restaurantId, CancellationToken cancellationToken);

    Task<SalesPeriodSummary> GetSalesBetween(int restaurantId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
