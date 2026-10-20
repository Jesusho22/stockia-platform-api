namespace DataBit.StockIA.Platform.Alerts.Domain.Model.Queries;

public record GetAlertsByRestaurantIdQuery(int RestaurantId);

public record GetRecommendationsByRestaurantIdQuery(int RestaurantId);
