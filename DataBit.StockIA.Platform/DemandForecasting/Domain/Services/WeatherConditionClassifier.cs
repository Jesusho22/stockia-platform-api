using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Services;

/// <summary>
///     Summarizes a day as SUNNY, CLOUDY, DRIZZLE or RAIN (the Web Application
///     translates the code). Uses the WMO code when Open-Meteo gives one.
/// </summary>
public static class WeatherConditionClassifier
{
    public static string Classify(DailyWeather weather)
    {
        if (weather.PrecipitationMm >= 1.0 || weather.WeatherCode is >= 61 and <= 67 or >= 80 and <= 82) return "RAIN";
        if (weather.PrecipitationMm >= 0.2 || weather.WeatherCode is >= 51 and <= 57) return "DRIZZLE";
        if (weather.WeatherCode is 2 or 3 or 45 or 48) return "CLOUDY";
        if (weather.WeatherCode is 0 or 1) return "SUNNY";
        // Climatology: Lima's sky is usually grey from June to October.
        return weather.Date.Month is >= 6 and <= 10 ? "CLOUDY" : "SUNNY";
    }
}
