namespace DataBit.StockIA.Platform.DemandForecasting.Interfaces.Rest.Resources;

public record DishUnitsResource(string DishName, int ProjectedUnits);

/// <summary>
///     One day of the forecast: projectedUnits is the total of every dish,
///     dishName the dish with the most units (what the Web Application chart
///     shows), and dishes the detail per dish.
/// </summary>
public record ForecastDayResource(string Date, string DayLabel, string DishName, int ProjectedUnits, double TempMaxC,
    double PrecipitationMm, IReadOnlyList<DishUnitsResource> Dishes);

/// <summary>Method: MACHINE_LEARNING or BASELINE. WeatherCondition: SUNNY, CLOUDY, DRIZZLE or RAIN.</summary>
public record DemandForecastResource(int Id, DateTimeOffset GeneratedAt, decimal ConfidenceScore, string WeatherCondition,
    string WeatherSource, string Method, string ModelVersion, IReadOnlyList<ForecastDayResource> DataPoints);

public record GenerateDemandForecastResource(int HorizonDays = 7);
