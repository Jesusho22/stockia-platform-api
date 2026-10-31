using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.MlRecommendations.Application.Services;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Queries;
using DataBit.StockIA.Platform.MlRecommendations.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.MlRecommendations.Interfaces.REST;

/// <summary>Recommendations generated from the demand forecast. Administrator only.</summary>
[Authorize]
[RequireRole("ADMIN")]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Purchase and menu recommendations (Administrator)")]
public class RecommendationsController(
    IRecommendationCommandService recommendationCommandService,
    IRecommendationQueryService recommendationQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List the recommendations (newest first)")]
    [EndpointName("GetRecommendations")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var recommendations = await recommendationQueryService.Handle(new GetRecommendationsByRestaurantIdQuery(currentUser.CurrentRestaurantId!.Value),
            cancellationToken);
        return Ok(recommendations.Select(item => RecommendationResourceFromEntityAssembler.ToResourceFromEntity(item)));
    }

    [HttpPost("{id:int}/apply")]
    [EndpointSummary("Mark a recommendation as applied")]
    [EndpointName("ApplyRecommendation")]
    public async Task<IActionResult> Apply([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await recommendationCommandService.Handle(new ApplyRecommendationCommand(id, currentUser.CurrentRestaurantId!.Value),
            cancellationToken);
        return MlRecommendationsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            recommendation => Ok(RecommendationResourceFromEntityAssembler.ToResourceFromEntity(recommendation)));
    }
}
