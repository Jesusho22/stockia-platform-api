using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;

/// <summary>
///     What a sale took out of one lot: quantity, unit cost and the lot's
///     expiration date at that moment. Analytics &amp; Dashboard uses it to see
///     how much stock was used before it expired (weekly savings).
/// </summary>
public class StockConsumption : IAuditableEntity
{
    private StockConsumption()
    {
    }

    public StockConsumption(int restaurantId, InventoryItem lot, Quantity quantity, DateOnly consumedOn)
    {
        if (!quantity.IsPositive) throw new ArgumentException("A consumption needs a quantity greater than zero.");
        RestaurantId = restaurantId;
        InventoryItemId = lot.Id;
        ItemName = lot.Name;
        Unit = lot.Unit;
        Quantity = quantity;
        UnitCost = lot.UnitCost;
        LotExpirationDate = lot.ExpirationDate;
        ConsumedOn = consumedOn;
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public int InventoryItemId { get; private set; }
    public string ItemName { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public Quantity Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public DateOnly LotExpirationDate { get; private set; }
    public DateOnly ConsumedOn { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public decimal Value => Math.Round(Quantity.ToDecimal() * UnitCost, 2);

    /// <summary>Days the lot still had left when it was used (0 = it expired that same day).</summary>
    public int DaysBeforeExpiration => LotExpirationDate.DayNumber - ConsumedOn.DayNumber;
}
