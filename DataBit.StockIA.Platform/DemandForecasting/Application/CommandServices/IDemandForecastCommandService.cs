using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.DemandForecasting.Application.CommandServices;

public interface IDemandForecastCommandService
{
    Task<Result<DemandForecast>> Handle(GenerateDemandForecastCommand command, CancellationToken cancellationToken);
}
