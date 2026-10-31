using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Internal.OutboundServices;

/// <summary>Daily weather forecast (Open-Meteo, with Lima climatology as fallback).</summary>
public interface IWeatherForecastService
{
    Task<IReadOnlyList<DailyWeather>> GetDailyForecastAsync(double latitude, double longitude, DateOnly from, int days,
        CancellationToken cancellationToken);
}
