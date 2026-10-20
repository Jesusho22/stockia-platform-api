using DataBit.StockIA.Platform.ProductInventory.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;

/// <summary>
///     A supply lot of the restaurant (e.g. 18 kg of chicken breast that
///     expires on a date). Two items with the same name and unit are two lots
///     of the same product: sales use the freshest lot that is not expired.
/// </summary>
public class InventoryItem : IAuditableEntity
{
    /// <summary>Days before the expiration date that count as "expiring soon".</summary>
    public const int ExpiringSoonDays = 3;

    private InventoryItem()
    {
    }

    public InventoryItem(int restaurantId, string name, string unit, Quantity quantity, Quantity minThreshold,
        StorageType storageType, int shelfLifeDays, decimal unitCost, DateOnly today)
    {
        RestaurantId = restaurantId;
        Update(name, unit, quantity, minThreshold, storageType, shelfLifeDays, unitCost, today);
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = "kg";
    public Quantity Quantity { get; private set; }
    public Quantity MinThreshold { get; private set; }
    public StorageType StorageType { get; private set; }
    public int ShelfLifeDays { get; private set; }

    /// <summary>Today (restaurant time zone) plus the shelf life, recalculated every time the lot is saved.</summary>
    public DateOnly ExpirationDate { get; private set; }

    /// <summary>Price of one unit (kg, L, unit) in soles.</summary>
    public decimal UnitCost { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string ProductKey => ProductKeyOf(Name, Unit);

    public decimal StockValue => Math.Round(Quantity.ToDecimal() * UnitCost, 2);

    public static string ProductKeyOf(string name, string unit)
    {
        static string Normalize(string text) => string.Join(' ', text.Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return $"{Normalize(name)}|{Normalize(unit)}";
    }

    public InventoryItem Update(string name, string unit, Quantity quantity, Quantity minThreshold, StorageType storageType,
        int shelfLifeDays, decimal unitCost, DateOnly today)
    {
        if (quantity.IsNegative || minThreshold.IsNegative) throw new ArgumentException("Quantities cannot be negative.");
        if (shelfLifeDays < 1) throw new ArgumentException("The shelf life must be at least one day.");
        if (unitCost < 0) throw new ArgumentException("The unit cost cannot be negative.");
        Name = name.Trim();
        Unit = unit.Trim();
        Quantity = quantity;
        MinThreshold = minThreshold;
        StorageType = storageType;
        ShelfLifeDays = shelfLifeDays;
        UnitCost = unitCost;
        ExpirationDate = today.AddDays(shelfLifeDays);
        return this;
    }

    /// <summary>Takes stock out of this lot; it never goes below zero.</summary>
    public InventoryItem Deduct(Quantity amount)
    {
        Quantity = Quantity.Max(Quantity.Zero, Quantity - amount);
        return this;
    }

    /// <summary>Only for seeding past data (lots registered days ago).</summary>
    public InventoryItem OverrideExpirationDate(DateOnly expirationDate)
    {
        ExpirationDate = expirationDate;
        return this;
    }

    public int DaysToExpire(DateOnly today) => ExpirationDate.DayNumber - today.DayNumber;

    public bool IsExpired(DateOnly today) => DaysToExpire(today) < 0;

    public bool IsExpiringSoon(DateOnly today) => DaysToExpire(today) is >= 0 and <= ExpiringSoonDays;

    public StockStatus StatusOn(DateOnly today)
    {
        if (IsExpired(today)) return StockStatus.Expired;
        if (!Quantity.IsPositive) return StockStatus.Critical;
        if (Quantity <= MinThreshold) return StockStatus.Low;
        return StockStatus.Available;
    }
}
