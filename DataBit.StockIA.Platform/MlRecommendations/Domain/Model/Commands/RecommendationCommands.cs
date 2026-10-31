namespace DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;

public record GenerateRecommendationsCommand(int RestaurantId, int DemandForecastId);

public record ApplyRecommendationCommand(int RecommendationId, int RestaurantId);
