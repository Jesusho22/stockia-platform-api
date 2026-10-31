namespace DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Queries;

public record GetDemandForecastsByRestaurantIdQuery(int RestaurantId, int Limit = 20);

public record GetLatestDemandForecastQuery(int RestaurantId);
