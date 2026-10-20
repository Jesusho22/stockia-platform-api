using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.DemandForecasting.Application.CommandServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.DemandForecasting.Application.QueryServices;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Commands;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Queries;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Services;
using DataBit.StockIA.Platform.DemandForecasting.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.DemandForecasting.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.DemandForecasting.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/demand-forecasts")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Demand forecast with Machine Learning")]
public class DemandForecastsController(
    IDemandForecastCommandService commandService,
    IDemandForecastQueryService queryService,
    IDemandModel demandModel,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    [HttpGet]
    [SwaggerOperation(Summary = "List the latest forecasts (newest first)", OperationId = "GetDemandForecasts")]
    public async Task<IActionResult> GetAll([FromQuery] int limit, CancellationToken cancellationToken)
    {
        var forecasts = await queryService.Handle(new GetDemandForecastsByRestaurantIdQuery(RestaurantId, limit == 0 ? 20 : limit),
            cancellationToken);
        return Ok(forecasts.Select(DemandForecastResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("latest")]
    [SwaggerOperation(Summary = "Get the latest forecast", OperationId = "GetLatestDemandForecast")]
    public async Task<IActionResult> GetLatest(CancellationToken cancellationToken)
    {
        var forecast = await queryService.Handle(new GetLatestDemandForecastQuery(RestaurantId), cancellationToken);
        return forecast == null ? NotFound() : Ok(DemandForecastResourceFromEntityAssembler.ToResourceFromEntity(forecast));
    }

    /// <summary>
    ///     Generates a new forecast for today and the next days with the ONNX
    ///     model, the sales history and the Open-Meteo weather forecast. It also
    ///     refreshes the purchase and menu recommendations.
    /// </summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Generate a forecast", OperationId = "GenerateDemandForecast")]
    [SwaggerResponse(StatusCodes.Status201Created, "Forecast generated", typeof(DemandForecastResource))]
    public async Task<IActionResult> Generate([FromBody] GenerateDemandForecastResource? resource, CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(new GenerateDemandForecastCommand(RestaurantId, resource?.HorizonDays ?? 7),
            cancellationToken);
        return DemandForecastActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            forecast => StatusCode(StatusCodes.Status201Created, DemandForecastResourceFromEntityAssembler.ToResourceFromEntity(forecast)));
    }

    /// <summary>Version, metrics and data sources of the model in use.</summary>
    [HttpGet("model")]
    [SwaggerOperation(Summary = "Describe the demand model", OperationId = "GetDemandModel")]
    public IActionResult GetModel() => Ok(new
    {
        demandModel.Info.Ready,
        demandModel.Info.ModelVersion,
        demandModel.Info.TrainedAt,
        demandModel.Info.Algorithm,
        demandModel.Info.DataSources,
        demandModel.Info.ConfidenceBase,
        demandModel.Info.Metrics,
        demandModel.Info.Problem,
        FeatureOrder = DemandFeatureBuilder.FeatureOrder,
        MinimumHistoryDays = Application.Internal.CommandServices.DemandForecastCommandService.MinimumHistoryDays
    });
}
