using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Resources;

public record RecipeIngredientResource(int InventoryItemId, string InventoryItemName, Quantity QuantityRequired, string Unit);

/// <summary>Price = sum of the ingredients (required quantity × unit cost of the freshest usable lot).</summary>
public record RecipeResource(int Id, string DishName, bool Active, IReadOnlyList<RecipeIngredientResource> Ingredients, decimal Price);
