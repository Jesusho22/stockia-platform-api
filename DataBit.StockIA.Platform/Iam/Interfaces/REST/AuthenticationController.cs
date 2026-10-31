using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Application.Services;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.Iam.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Iam.Resources;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST;

/// <summary>
///     Login Controller and Password Reset Service (C4). The sign-up of a new
///     restaurant is in Restaurant Registration (POST /api/v1/restaurant-registrations).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Sign-in and password recovery")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    ExternalRestaurantService externalRestaurantService,
    IStringLocalizer<IamMessages> localizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Authenticates with email and password and returns the user with a JWT.
    ///     After 5 wrong passwords in a row the account is locked for 15 minutes (423 AccountLocked).
    /// </summary>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [EndpointSummary("Sign in")]
    [EndpointName("SignIn")]
    [ProducesResponseType(typeof(AuthenticatedUserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> SignIn([FromBody] SignInResource resource, CancellationToken cancellationToken)
    {
        var result = await userCommandService.Handle(SignInCommandFromResourceAssembler.ToCommandFromResource(resource), cancellationToken);
        if (result.IsFailure) return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, _ => Ok());

        var (user, token) = result.Value;
        var restaurant = await externalRestaurantService.GetRestaurant(user.RestaurantId, cancellationToken);
        return Ok(UserResourceFromEntityAssembler.ToAuthenticatedResource(user, restaurant, token));
    }

    /// <summary>"Forgot your password?": emails a recovery link. Same answer whether the email is registered or not.</summary>
    [HttpPost("password-reset-requests")]
    [AllowAnonymous]
    [EndpointSummary("Request a password recovery link")]
    [EndpointName("RequestPasswordReset")]
    [ProducesResponseType(typeof(PasswordResetRequestResource), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetResource resource, CancellationToken cancellationToken)
    {
        var result = await userCommandService.Handle(PasswordResetCommandFromResourceAssembler.ToRequestCommand(resource), cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, request =>
            StatusCode(StatusCodes.Status202Accepted,
                PasswordResetRequestResourceFromValueAssembler.ToResource(request, localizer["PasswordResetRequested"])));
    }

    /// <summary>Sets a new password with the code of the recovery link (it also unlocks the account).</summary>
    [HttpPost("password-resets")]
    [AllowAnonymous]
    [EndpointSummary("Reset the password")]
    [EndpointName("ResetPassword")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordResource resource, CancellationToken cancellationToken)
    {
        var result = await userCommandService.Handle(PasswordResetCommandFromResourceAssembler.ToResetCommand(resource), cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }
}
