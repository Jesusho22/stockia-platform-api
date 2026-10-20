using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Alerts.Application.CommandServices;
using DataBit.StockIA.Platform.Alerts.Application.QueryServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Queries;
using DataBit.StockIA.Platform.Alerts.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Alerts.Interfaces.Rest;

/// <summary>Recommendations generated from the demand forecast. Administrator only.</summary>
[Authorize]
[RequireRole("ADMIN")]
[ApiController]
[Route("api/v1/recommendations")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Purchase and menu recommendations (Administrator)")]
public class RecommendationsController(
    IRecommendationCommandService recommendationCommandService,
    IAlertQueryService alertQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List the recommendations (newest first)", OperationId = "GetRecommendations")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var recommendations = await alertQueryService.Handle(new GetRecommendationsByRestaurantIdQuery(currentUser.CurrentRestaurantId!.Value),
            cancellationToken);
        return Ok(recommendations.Select(item => AlertResourceFromEntityAssembler.ToResourceFromEntity(item)));
    }

    [HttpPost("{id:int}/apply")]
    [SwaggerOperation(Summary = "Mark a recommendation as applied", OperationId = "ApplyRecommendation")]
    public async Task<IActionResult> Apply([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await recommendationCommandService.Handle(new ApplyRecommendationCommand(id, currentUser.CurrentRestaurantId!.Value),
            cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            recommendation => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(recommendation)));
    }
}
