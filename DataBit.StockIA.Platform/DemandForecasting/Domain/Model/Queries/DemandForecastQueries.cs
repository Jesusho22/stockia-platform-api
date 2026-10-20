namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Queries;

public record GetDemandForecastsByRestaurantIdQuery(int RestaurantId, int Limit = 20);

public record GetLatestDemandForecastQuery(int RestaurantId);
