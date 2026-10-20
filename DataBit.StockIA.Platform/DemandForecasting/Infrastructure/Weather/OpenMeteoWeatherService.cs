using System.Globalization;
using System.Text.Json;
using DataBit.StockIA.Platform.DemandForecasting.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.DemandForecasting.Infrastructure.Weather;

/// <summary>
///     Open-Meteo forecast API (https://open-meteo.com): free, no API key.
///     Any network error or missing day falls back to Lima's climatology, so a
///     forecast is always produced.
/// </summary>
public class OpenMeteoWeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenMeteoWeatherService> logger)
    : IWeatherForecastService
{
    public async Task<IReadOnlyList<DailyWeather>> GetDailyForecastAsync(double latitude, double longitude, DateOnly from, int days,
        CancellationToken cancellationToken)
    {
        var to = from.AddDays(days - 1);
        var result = Enumerable.Range(0, days).Select(offset => LimaClimatology.For(from.AddDays(offset))).ToList();
        if (!bool.TryParse(configuration["Weather:Enabled"], out var enabled) || !enabled) return result;

        var url = string.Format(CultureInfo.InvariantCulture,
            "{0}?latitude={1}&longitude={2}&daily=temperature_2m_max,precipitation_sum,weather_code&timezone=America%2FLima&start_date={3:yyyy-MM-dd}&end_date={4:yyyy-MM-dd}",
            configuration["Weather:ForecastUrl"] ?? "https://api.open-meteo.com/v1/forecast", latitude, longitude, from, to);
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var daily = document.RootElement.GetProperty("daily");
            var dates = daily.GetProperty("time").EnumerateArray().Select(item => DateOnly.Parse(item.GetString()!, CultureInfo.InvariantCulture)).ToList();
            var temps = daily.GetProperty("temperature_2m_max").EnumerateArray().ToList();
            var rains = daily.GetProperty("precipitation_sum").EnumerateArray().ToList();
            var codes = daily.GetProperty("weather_code").EnumerateArray().ToList();
            for (var index = 0; index < dates.Count; index++)
            {
                var position = result.FindIndex(day => day.Date == dates[index]);
                if (position < 0 || temps[index].ValueKind != JsonValueKind.Number) continue;
                result[position] = new DailyWeather(dates[index], temps[index].GetDouble(),
                    rains[index].ValueKind == JsonValueKind.Number ? rains[index].GetDouble() : 0,
                    codes[index].ValueKind == JsonValueKind.Number ? codes[index].GetInt32() : null, "open-meteo");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException
                                              or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            logger.LogWarning("Open-Meteo not available ({Message}); using Lima climatology", exception.Message);
        }

        return result;
    }
}
