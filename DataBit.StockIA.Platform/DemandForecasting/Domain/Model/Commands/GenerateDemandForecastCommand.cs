namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Commands;

public record GenerateDemandForecastCommand(int RestaurantId, int HorizonDays = 7);
