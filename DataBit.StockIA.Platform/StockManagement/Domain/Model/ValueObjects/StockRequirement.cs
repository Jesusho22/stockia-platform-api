using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;

/// <summary>
///     What a dish needs from one supply per portion. Sent by Recipes Management
///     through the ACL; the name and unit find other lots of the same product.
/// </summary>
public record StockRequirement(int InventoryItemId, string ItemName, string Unit, Quantity QuantityPerPortion);
