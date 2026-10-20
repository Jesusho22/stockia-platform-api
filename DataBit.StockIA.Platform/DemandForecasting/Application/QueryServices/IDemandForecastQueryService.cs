using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.DemandForecasting.Application.QueryServices;

public interface IDemandForecastQueryService
{
    Task<IReadOnlyList<DemandForecast>> Handle(GetDemandForecastsByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<DemandForecast?> Handle(GetLatestDemandForecastQuery query, CancellationToken cancellationToken);
}
