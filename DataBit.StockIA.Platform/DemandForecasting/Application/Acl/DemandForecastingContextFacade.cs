using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Repositories;
using DataBit.StockIA.Platform.DemandForecasting.Interfaces.Acl;

namespace DataBit.StockIA.Platform.DemandForecasting.Application.Acl;

public class DemandForecastingContextFacade(IDemandForecastRepository repository) : IDemandForecastingContextFacade
{
    public async Task<ForecastSummary?> GetForecast(int restaurantId, int forecastId, CancellationToken cancellationToken)
    {
        var forecast = await repository.FindByIdAsync(forecastId, cancellationToken);
        return forecast == null || forecast.RestaurantId != restaurantId ? null : ToSummary(forecast);
    }

    public async Task<ForecastSummary?> GetLatestForecast(int restaurantId, CancellationToken cancellationToken)
    {
        var forecast = await repository.FindLatestByRestaurantIdAsync(restaurantId, cancellationToken);
        return forecast == null ? null : ToSummary(forecast);
    }

    private static ForecastSummary ToSummary(DemandForecast forecast)
    {
        var dishes = forecast.DataPoints.GroupBy(point => point.DishName)
            .Select(group => new DishProjection(group.Key, group.Sum(point => point.ProjectedUnits),
                group.ToDictionary(point => point.Date, point => point.ProjectedUnits)))
            .OrderByDescending(dish => dish.TotalUnits).ToList();
        var dates = forecast.DataPoints.Select(point => point.Date).DefaultIfEmpty(DateOnly.FromDateTime(forecast.GeneratedAt.Date)).ToList();
        return new ForecastSummary(forecast.Id, forecast.GeneratedAt, dates.Min(), dates.Max(), forecast.ConfidenceScore, dishes);
    }
}
