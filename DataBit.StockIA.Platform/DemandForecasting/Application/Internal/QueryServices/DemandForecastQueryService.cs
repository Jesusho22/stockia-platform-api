using DataBit.StockIA.Platform.DemandForecasting.Application.QueryServices;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Queries;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Repositories;

namespace DataBit.StockIA.Platform.DemandForecasting.Application.Internal.QueryServices;

public class DemandForecastQueryService(IDemandForecastRepository repository) : IDemandForecastQueryService
{
    public Task<IReadOnlyList<DemandForecast>> Handle(GetDemandForecastsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        repository.FindRecentByRestaurantIdAsync(query.RestaurantId, Math.Clamp(query.Limit, 1, 100), cancellationToken);

    public Task<DemandForecast?> Handle(GetLatestDemandForecastQuery query, CancellationToken cancellationToken) =>
        repository.FindLatestByRestaurantIdAsync(query.RestaurantId, cancellationToken);
}
