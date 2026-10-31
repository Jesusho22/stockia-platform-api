using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Services;

public interface IDemandForecastQueryService
{
    Task<IReadOnlyList<DemandForecast>> Handle(GetDemandForecastsByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<DemandForecast?> Handle(GetLatestDemandForecastQuery query, CancellationToken cancellationToken);
}
