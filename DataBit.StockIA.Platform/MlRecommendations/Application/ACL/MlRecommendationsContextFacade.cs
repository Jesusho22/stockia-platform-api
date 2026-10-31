using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Repositories;
using DataBit.StockIA.Platform.MlRecommendations.Interfaces.ACL;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.ACL;

public class MlRecommendationsContextFacade(IDemandForecastRepository repository) : IMlRecommendationsContextFacade
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
