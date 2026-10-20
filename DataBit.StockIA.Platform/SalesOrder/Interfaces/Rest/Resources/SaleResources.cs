using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.SalesOrder.Interfaces.Rest.Resources;

public record SaleLineItemResource(int RecipeId, string DishName, decimal UnitPrice, int Quantity);

/// <summary>Channel: POS or MANUAL. Status: CONFIRMED or VOIDED.</summary>
public record SaleResource(int Id, DateTimeOffset SaleDate, string Channel, string Status, IReadOnlyList<SaleLineItemResource> LineItems,
    decimal Total);

/// <summary>Sells portions of a recipe; the price is calculated by the server.</summary>
public record RegisterSaleResource([Range(1, int.MaxValue)] int RecipeId, [Range(1, 500)] int Quantity = 1, string Channel = "POS");
