namespace DataBit.StockIA.Platform.MlRecommendations.Interfaces.ACL;

/// <summary>Projected units of a dish over the forecast horizon, and per day.</summary>
public record DishProjection(string DishName, int TotalUnits, IReadOnlyDictionary<DateOnly, int> UnitsByDay);

public record ForecastSummary(int ForecastId, DateTimeOffset GeneratedAt, DateOnly From, DateOnly To, decimal ConfidenceScore,
    IReadOnlyList<DishProjection> Dishes);

/// <summary>What other bounded contexts may read from ML &amp; Recommendations.</summary>
public interface IMlRecommendationsContextFacade
{
    Task<ForecastSummary?> GetForecast(int restaurantId, int forecastId, CancellationToken cancellationToken);
    Task<ForecastSummary?> GetLatestForecast(int restaurantId, CancellationToken cancellationToken);
}
