namespace DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;

public record GenerateDemandForecastCommand(int RestaurantId, int HorizonDays = 7);
