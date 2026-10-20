namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.Queries;

public record GetInventoryItemsByRestaurantIdQuery(int RestaurantId);

public record GetInventoryItemByIdQuery(int InventoryItemId, int RestaurantId);

public record GetRecipesByRestaurantIdQuery(int RestaurantId);

public record GetRecipeByIdQuery(int RecipeId, int RestaurantId);
