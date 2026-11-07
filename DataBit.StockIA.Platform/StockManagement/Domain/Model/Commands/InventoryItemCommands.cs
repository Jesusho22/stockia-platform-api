using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.StockManagement.Domain.Model.Commands;

public record CreateInventoryItemCommand(
    int RestaurantId, string Name, string Unit, Quantity Quantity, Quantity MinThreshold, StorageType StorageType,
    int ShelfLifeDays, decimal UnitCost);

public record UpdateInventoryItemCommand(
    int InventoryItemId, int RestaurantId, string Name, string Unit, Quantity Quantity, Quantity MinThreshold,
    StorageType StorageType, int ShelfLifeDays, decimal UnitCost);

public record DeleteInventoryItemCommand(int InventoryItemId, int RestaurantId);

/// <summary>
///     Takes out of the inventory what a sold dish consumes (sent by Recipes
///     Management's Ingredient Deduction Engine through the ACL).
/// </summary>
public record DeductStockCommand(int RestaurantId, IReadOnlyList<StockRequirement> Requirements, int Portions);
