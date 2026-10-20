using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Entities;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;

/// <summary>
///     Projected units per dish for the next days (7 by default), generated
///     from the sales history, the Peruvian calendar and the weather forecast.
/// </summary>
public class DemandForecast : IAuditableEntity
{
    private readonly List<ForecastDataPoint> _dataPoints = [];

    private DemandForecast()
    {
    }

    public DemandForecast(int restaurantId, DateTimeOffset generatedAt, decimal confidenceScore, string weatherCondition,
        string weatherSource, ForecastMethod method, string modelVersion, IEnumerable<ForecastDataPoint> dataPoints)
    {
        RestaurantId = restaurantId;
        GeneratedAt = generatedAt;
        ConfidenceScore = Math.Clamp(confidenceScore, 0m, 1m);
        WeatherCondition = weatherCondition;
        WeatherSource = weatherSource;
        Method = method;
        ModelVersion = modelVersion;
        _dataPoints.AddRange(dataPoints);
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Between 0 and 1: model accuracy (1 - WAPE) × how complete the recent sales history is.</summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>SUNNY, CLOUDY, DRIZZLE or RAIN for the first forecast day.</summary>
    public string WeatherCondition { get; private set; } = string.Empty;

    public string WeatherSource { get; private set; } = string.Empty;
    public ForecastMethod Method { get; private set; }
    public string ModelVersion { get; private set; } = string.Empty;
    public IReadOnlyList<ForecastDataPoint> DataPoints => _dataPoints;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Total projected units per dish over the whole horizon.</summary>
    public IReadOnlyDictionary<string, int> UnitsByDish() =>
        _dataPoints.GroupBy(point => point.DishName).ToDictionary(group => group.Key, group => group.Sum(point => point.ProjectedUnits));
}
