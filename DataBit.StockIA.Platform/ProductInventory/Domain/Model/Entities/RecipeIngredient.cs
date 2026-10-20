using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.Entities;

/// <summary>
///     A supply a dish consumes per portion. Linked to a lot by id; the name
///     and unit let the allocation service find other lots of the same product.
/// </summary>
public class RecipeIngredient
{
    private RecipeIngredient()
    {
    }

    public RecipeIngredient(int inventoryItemId, string inventoryItemName, Quantity quantityRequired, string unit)
    {
        if (!quantityRequired.IsPositive) throw new ArgumentException("The required quantity must be greater than zero.");
        InventoryItemId = inventoryItemId;
        InventoryItemName = inventoryItemName;
        QuantityRequired = quantityRequired;
        Unit = unit;
    }

    public int InventoryItemId { get; private set; }
    public string InventoryItemName { get; private set; } = string.Empty;
    public Quantity QuantityRequired { get; private set; }
    public string Unit { get; private set; } = string.Empty;
}
