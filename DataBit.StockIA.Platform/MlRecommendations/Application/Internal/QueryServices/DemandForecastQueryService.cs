using DataBit.StockIA.Platform.MlRecommendations.Application.Services;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Queries;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Repositories;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Internal.QueryServices;

public class DemandForecastQueryService(IDemandForecastRepository repository) : IDemandForecastQueryService
{
    public Task<IReadOnlyList<DemandForecast>> Handle(GetDemandForecastsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        repository.FindRecentByRestaurantIdAsync(query.RestaurantId, Math.Clamp(query.Limit, 1, 100), cancellationToken);

    public Task<DemandForecast?> Handle(GetLatestDemandForecastQuery query, CancellationToken cancellationToken) =>
        repository.FindLatestByRestaurantIdAsync(query.RestaurantId, cancellationToken);
}
