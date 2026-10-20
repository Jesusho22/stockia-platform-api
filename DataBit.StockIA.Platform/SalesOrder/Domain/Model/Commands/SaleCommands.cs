using DataBit.StockIA.Platform.SalesOrder.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SalesOrder.Domain.Model.Commands;

/// <summary>Sells portions of a dish: validates stock, prices it, records it and discounts the supplies.</summary>
public record RegisterSaleCommand(int RestaurantId, int RecipeId, int Quantity, SaleChannel Channel);

public record VoidSaleCommand(int SaleId, int RestaurantId);
