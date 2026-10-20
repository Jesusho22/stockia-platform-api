namespace DataBit.StockIA.Platform.SalesOrder.Domain.Model.Queries;

public record GetSalesByRestaurantIdQuery(int RestaurantId, DateOnly? From = null, DateOnly? To = null);

public record GetSaleByIdQuery(int SaleId, int RestaurantId);
