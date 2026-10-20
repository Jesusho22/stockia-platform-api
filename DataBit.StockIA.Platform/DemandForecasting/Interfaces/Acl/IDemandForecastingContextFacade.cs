namespace DataBit.StockIA.Platform.DemandForecasting.Interfaces.Acl;

/// <summary>Projected units of a dish over the forecast horizon, and per day.</summary>
public record DishProjection(string DishName, int TotalUnits, IReadOnlyDictionary<DateOnly, int> UnitsByDay);

public record ForecastSummary(int ForecastId, DateTimeOffset GeneratedAt, DateOnly From, DateOnly To, decimal ConfidenceScore,
    IReadOnlyList<DishProjection> Dishes);

/// <summary>What other bounded contexts may read from Demand Forecasting.</summary>
public interface IDemandForecastingContextFacade
{
    Task<ForecastSummary?> GetForecast(int restaurantId, int forecastId, CancellationToken cancellationToken);
    Task<ForecastSummary?> GetLatestForecast(int restaurantId, CancellationToken cancellationToken);
}
