using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Services;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Queries;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST;

/// <summary>
///     Restaurant data (C4: Profile Management Component). Every member reads
///     it; only the Administrator changes it.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Restaurant data")]
public class RestaurantsController(
    IRestaurantCommandService restaurantCommandService,
    IRestaurantQueryService restaurantQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("current")]
    [EndpointSummary("Get the restaurant of the signed-in user")]
    [EndpointName("GetCurrentRestaurant")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken);
        return restaurant == null ? NotFound() : Ok(RestaurantResourceFromEntityAssembler.ToResourceFromEntity(restaurant));
    }

    [HttpPut("current")]
    [RequireRole("ADMIN")]
    [EndpointSummary("Update the restaurant data")]
    [EndpointName("UpdateCurrentRestaurant")]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateRestaurantResource resource, CancellationToken cancellationToken)
    {
        var command = UpdateRestaurantCommandFromResourceAssembler.ToCommandFromResource(resource, currentUser.CurrentRestaurantId!.Value);
        var result = await restaurantCommandService.Handle(command, cancellationToken);
        return RestaurantRegistrationActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            restaurant => Ok(RestaurantResourceFromEntityAssembler.ToResourceFromEntity(restaurant)));
    }
}
