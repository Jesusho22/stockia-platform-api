using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;

/// <summary>What a dish needs from one supply per portion (quantity as an exact value).</summary>
public record IngredientRequirement(int InventoryItemId, string ItemName, string Unit, Quantity QuantityPerPortion);

/// <summary>A supply that blocks a sale: reason NOT_FOUND, EXPIRED or INSUFFICIENT; quantities as exact text.</summary>
public record ShortageInfo(string ItemName, string Unit, string Reason, string Required, string Available, string Expired);

/// <summary>Whether a dish can be prepared now, and its price (sum of its ingredients).</summary>
public record StockAvailability(bool CanFulfill, decimal UnitPrice, IReadOnlyList<ShortageInfo> Shortages);

/// <summary>A supply lot as other contexts see it. Status: AVAILABLE, LOW, CRITICAL or EXPIRED.</summary>
public record InventoryLotInfo(
    int InventoryItemId, string Name, string Unit, decimal Quantity, decimal MinThreshold, DateOnly ExpirationDate,
    int DaysToExpire, decimal UnitCost, decimal StockValue, string Status, string ProductKey);

/// <summary>Stock a sale took out of a lot, with the days the lot still had left.</summary>
public record StockConsumptionInfo(DateOnly ConsumedOn, string ItemName, string Unit, decimal Quantity, decimal Value,
    int DaysBeforeExpiration);

/// <summary>
///     The only entry point of other bounded contexts into Stock Management
///     (never its repositories or tables).
/// </summary>
public interface IStockManagementContextFacade
{
    /// <summary>Checks the stock for some portions of a dish (expired lots excluded) — Recipes Management.</summary>
    Task<StockAvailability> CheckAvailability(int restaurantId, IReadOnlyList<IngredientRequirement> ingredients, int portions,
        CancellationToken cancellationToken);

    /// <summary>Price of one portion of each dish: sum of its ingredients at the freshest usable lot cost.</summary>
    Task<IReadOnlyList<decimal>> PricesOf(int restaurantId, IReadOnlyList<IReadOnlyList<IngredientRequirement>> dishes,
        CancellationToken cancellationToken);

    /// <summary>Takes out the supplies of a sold dish. Returns the shortages when it could not (nothing changes then).</summary>
    Task<IReadOnlyList<ShortageInfo>> DeductStock(int restaurantId, IReadOnlyList<IngredientRequirement> ingredients, int portions,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryLotInfo>> GetInventory(int restaurantId, CancellationToken cancellationToken);

    Task<InventoryLotInfo?> GetInventoryLot(int restaurantId, int inventoryItemId, CancellationToken cancellationToken);

    /// <summary>Stock used by sales between two local dates (inclusive) — Analytics &amp; Dashboard.</summary>
    Task<IReadOnlyList<StockConsumptionInfo>> GetConsumptions(int restaurantId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken);
}
