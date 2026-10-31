using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Patterns;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Services;

public interface IDemandForecastCommandService
{
    Task<Result<DemandForecast>> Handle(GenerateDemandForecastCommand command, CancellationToken cancellationToken);
}
