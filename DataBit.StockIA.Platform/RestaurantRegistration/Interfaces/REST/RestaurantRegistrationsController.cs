using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Services;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST;

/// <summary>
///     Restaurant onboarding (C4: Restaurant Onboarding Controller): registers
///     a restaurant with its owner as the first Administrator and the free
///     plan, and signs the owner in.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Restaurant registration (sign-up)")]
public class RestaurantRegistrationsController(
    IRestaurantCommandService restaurantCommandService,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EndpointSummary("Register a restaurant and its owner")]
    [EndpointName("RegisterRestaurant")]
    [ProducesResponseType(typeof(RegisteredOwnerResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRestaurantResource resource, CancellationToken cancellationToken)
    {
        var result = await restaurantCommandService.Handle(RegisterRestaurantCommandFromResourceAssembler.ToCommandFromResource(resource),
            cancellationToken);
        return RestaurantRegistrationActionResultAssembler.ToActionResult(result, problemDetailsFactory, registration =>
            StatusCode(StatusCodes.Status201Created, RegisteredOwnerResourceFromResultAssembler.ToResourceFromResult(registration)));
    }
}
