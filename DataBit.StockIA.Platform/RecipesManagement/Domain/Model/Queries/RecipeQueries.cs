namespace DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Queries;

public record GetRecipesByRestaurantIdQuery(int RestaurantId);

public record GetRecipeByIdQuery(int RecipeId, int RestaurantId);
