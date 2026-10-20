using DataBit.StockIA.Platform.ProductInventory.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;

public record CreateInventoryItemCommand(
    int RestaurantId, string Name, string Unit, Quantity Quantity, Quantity MinThreshold, StorageType StorageType,
    int ShelfLifeDays, decimal UnitCost);

public record UpdateInventoryItemCommand(
    int InventoryItemId, int RestaurantId, string Name, string Unit, Quantity Quantity, Quantity MinThreshold,
    StorageType StorageType, int ShelfLifeDays, decimal UnitCost);

public record DeleteInventoryItemCommand(int InventoryItemId, int RestaurantId);
