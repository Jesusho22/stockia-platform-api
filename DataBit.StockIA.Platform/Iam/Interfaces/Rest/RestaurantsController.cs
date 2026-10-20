using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Application.CommandServices;
using DataBit.StockIA.Platform.Iam.Application.QueryServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest;

/// <summary>
///     Restaurant data (Account settings). Every member reads it; only the
///     Administrator changes it.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/restaurants")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Restaurant data")]
public class RestaurantsController(
    IRestaurantCommandService restaurantCommandService,
    IRestaurantQueryService restaurantQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("current")]
    [SwaggerOperation(Summary = "Get the restaurant of the signed-in user", OperationId = "GetCurrentRestaurant")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken);
        return restaurant == null ? NotFound() : Ok(RestaurantResourceFromEntityAssembler.ToResourceFromEntity(restaurant));
    }

    [HttpPut("current")]
    [RequireRole("ADMIN")]
    [SwaggerOperation(Summary = "Update the restaurant data", OperationId = "UpdateCurrentRestaurant")]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateRestaurantResource resource, CancellationToken cancellationToken)
    {
        var command = UpdateRestaurantCommandFromResourceAssembler.ToCommandFromResource(resource, currentUser.CurrentRestaurantId!.Value);
        var result = await restaurantCommandService.Handle(command, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            restaurant => Ok(RestaurantResourceFromEntityAssembler.ToResourceFromEntity(restaurant)));
    }
}
