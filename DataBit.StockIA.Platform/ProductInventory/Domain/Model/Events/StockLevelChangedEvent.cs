using DataBit.StockIA.Platform.Shared.Domain.Model.Events;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.Events;

/// <summary>
///     Raised when a supply lot is created, edited or loses stock by a sale.
///     Alerts &amp; Recommendations reacts creating LOW_STOCK, CRITICAL_STOCK or
///     EXPIRING_SOON alerts.
/// </summary>
public record StockLevelChangedEvent(int RestaurantId, int InventoryItemId) : IEvent;
