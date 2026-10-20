using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Resources;

/// <summary>
///     A supply lot. Quantities are strings with up to 32 decimals; status
///     (AVAILABLE, LOW, CRITICAL, EXPIRED) and stockValue are calculated.
/// </summary>
public record InventoryItemResource(
    int Id, string Name, string Unit, Quantity Quantity, Quantity MinThreshold, string StorageType, int ShelfLifeDays,
    string ExpirationDate, decimal UnitCost, decimal StockValue, string Status);
