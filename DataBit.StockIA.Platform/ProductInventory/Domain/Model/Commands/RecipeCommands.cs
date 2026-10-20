using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;

/// <summary>Ingredient line sent by the client; name and unit are taken from the inventory.</summary>
public record RecipeIngredientData(int InventoryItemId, Quantity QuantityRequired);

public record CreateRecipeCommand(int RestaurantId, string DishName, IReadOnlyList<RecipeIngredientData> Ingredients);

public record UpdateRecipeCommand(int RecipeId, int RestaurantId, string DishName, IReadOnlyList<RecipeIngredientData> Ingredients);

public record DeleteRecipeCommand(int RecipeId, int RestaurantId);

/// <summary>RecipeStockDeductionService: discounts the supplies of a sold dish (called through the ACL by Sales).</summary>
public record DeductStockForRecipeCommand(int RestaurantId, int RecipeId, int Portions);
