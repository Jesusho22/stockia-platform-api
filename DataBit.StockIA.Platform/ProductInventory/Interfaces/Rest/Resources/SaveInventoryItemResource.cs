using System.ComponentModel.DataAnnotations;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Resources;

/// <summary>
///     Body to create or edit a supply. Quantity and minThreshold accept a JSON
///     number or a string with up to 32 decimals. The expiration date is
///     calculated by the server (today + shelfLifeDays).
/// </summary>
public record SaveInventoryItemResource(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(20)] string Unit,
    Quantity Quantity,
    Quantity MinThreshold,
    [Required] string StorageType,
    [Range(1, 3650)] int ShelfLifeDays,
    [Range(0, 1_000_000)] decimal UnitCost);
