using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.DemandForecasting.Infrastructure.Weather;

/// <summary>
///     Monthly climatology of Lima (mean daily max °C, mean daily mm). Same
///     table as ml/stockia_ml/config.py; used when Open-Meteo is not reachable.
/// </summary>
public static class LimaClimatology
{
    private static readonly (double TempMaxC, double PrecipitationMm)[] Months =
    [
        (26.5, 0.05), (27.5, 0.10), (27.0, 0.05), (25.0, 0.02), (22.5, 0.05), (20.0, 0.15),
        (19.0, 0.25), (18.5, 0.30), (19.0, 0.20), (20.5, 0.10), (22.5, 0.05), (24.5, 0.05)
    ];

    public static DailyWeather For(DateOnly date)
    {
        var (temp, rain) = Months[date.Month - 1];
        return new DailyWeather(date, temp, rain, null, "climatology");
    }
}
