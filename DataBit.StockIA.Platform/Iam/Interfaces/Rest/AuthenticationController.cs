using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Application.CommandServices;
using DataBit.StockIA.Platform.Iam.Application.QueryServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest;

[ApiController]
[Route("api/v1/authentication")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Sign-in and sign-up")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IRestaurantQueryService restaurantQueryService,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>Authenticates with email and password and returns the user with a JWT.</summary>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Sign in", OperationId = "SignIn")]
    [SwaggerResponse(StatusCodes.Status200OK, "Authenticated", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid credentials")]
    public async Task<IActionResult> SignIn([FromBody] SignInResource resource, CancellationToken cancellationToken)
    {
        var result = await userCommandService.Handle(SignInCommandFromResourceAssembler.ToCommandFromResource(resource), cancellationToken);
        if (result.IsFailure) return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, _ => Ok());

        var (user, token) = result.Value;
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(user.RestaurantId), cancellationToken);
        return Ok(UserResourceFromEntityAssembler.ToAuthenticatedResource(user, restaurant, token));
    }

    /// <summary>
    ///     Creates the restaurant and its owner (Administrator) with the free
    ///     plan, and signs in right away.
    /// </summary>
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Sign up", OperationId = "SignUp")]
    [SwaggerResponse(StatusCodes.Status201Created, "Account created", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email already registered")]
    public async Task<IActionResult> SignUp([FromBody] SignUpResource resource, CancellationToken cancellationToken)
    {
        var result = await userCommandService.Handle(SignUpCommandFromResourceAssembler.ToCommandFromResource(resource), cancellationToken);
        if (result.IsFailure) return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, _ => Ok());

        var (user, token) = result.Value;
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(user.RestaurantId), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, UserResourceFromEntityAssembler.ToAuthenticatedResource(user, restaurant, token));
    }
}
