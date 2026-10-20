using System.ComponentModel.DataAnnotations;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Resources;

public record SaveRecipeIngredientResource([Range(1, int.MaxValue)] int InventoryItemId, Quantity QuantityRequired);

public record SaveRecipeResource([Required, MaxLength(120)] string DishName, [Required, MinLength(1)] IReadOnlyList<SaveRecipeIngredientResource> Ingredients);
