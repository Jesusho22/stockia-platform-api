namespace DataBit.StockIA.Platform.StockManagement.Domain.Model.Queries;

public record GetInventoryItemsByRestaurantIdQuery(int RestaurantId);

public record GetInventoryItemByIdQuery(int InventoryItemId, int RestaurantId);
