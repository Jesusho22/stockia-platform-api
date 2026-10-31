using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Application.Services;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.Iam.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST;

/// <summary>
///     Team of the restaurant (Roles &amp; permissions) and the profile of the
///     signed-in user. Listing and changing the team is for Administrators;
///     every user edits only their own profile and password.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Team members and profile")]
public class UsersController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService,
    ExternalRestaurantService externalRestaurantService,
    ICurrentUserAccessor currentUser,
    IRestaurantClock clock,
    IConfiguration configuration,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [RequireRole("ADMIN")]
    [EndpointSummary("List the team of the restaurant (newest first)")]
    [EndpointName("GetUsers")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var restaurantId = currentUser.CurrentRestaurantId!.Value;
        var restaurant = await externalRestaurantService.GetRestaurant(restaurantId, cancellationToken);
        var users = await userQueryService.Handle(new GetUsersByRestaurantIdQuery(restaurantId), cancellationToken);
        return Ok(users.Select(user => UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant, clock.Now)));
    }

    [HttpGet("me")]
    [EndpointSummary("Get the signed-in user")]
    [EndpointName("GetCurrentUser")]
    public Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken) =>
        GetUserById(currentUser.CurrentUserId!.Value, cancellationToken);

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a team member")]
    [EndpointName("GetUserById")]
    public async Task<IActionResult> GetUserById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (user == null || user.RestaurantId != currentUser.CurrentRestaurantId) return NotFound();
        if (!currentUser.IsAdmin && user.Id != currentUser.CurrentUserId) return ForbiddenProblem();
        var restaurant = await externalRestaurantService.GetRestaurant(user.RestaurantId, cancellationToken);
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant, clock.Now));
    }

    /// <summary>Invites a member with a temporary password (a real deployment would email it).</summary>
    [HttpPost]
    [RequireRole("ADMIN")]
    [EndpointSummary("Invite a team member")]
    [EndpointName("InviteMember")]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteMember([FromBody] InviteMemberResource resource, CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<UserRole>(resource.Role, out var role)) return ValidationRole();
        var restaurantId = currentUser.CurrentRestaurantId!.Value;
        var command = InviteMemberCommandFromResourceAssembler.ToCommandFromResource(resource, restaurantId, role,
            configuration["Iam:InvitationTemporaryPassword"] ?? "stockia123");
        var result = await userCommandService.Handle(command, cancellationToken);
        var restaurant = await externalRestaurantService.GetRestaurant(restaurantId, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, user =>
            CreatedAtAction(nameof(GetUserById), new { id = user.Id }, UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant, clock.Now)));
    }

    /// <summary>Profile data (name and email) of the signed-in user.</summary>
    [HttpPatch("{id:int}")]
    [EndpointSummary("Update my profile")]
    [EndpointName("UpdateUserProfile")]
    public async Task<IActionResult> UpdateProfile([FromRoute] int id, [FromBody] UpdateUserProfileResource resource,
        CancellationToken cancellationToken)
    {
        if (id != currentUser.CurrentUserId) return ForbiddenProblem();
        var result = await userCommandService.Handle(UpdateUserProfileCommandFromResourceAssembler.ToCommandFromResource(resource, id),
            cancellationToken);
        var restaurant = await externalRestaurantService.GetRestaurant(currentUser.CurrentRestaurantId!.Value, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            user => Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant, clock.Now)));
    }

    /// <summary>Changes my password after verifying the current one.</summary>
    [HttpPost("{id:int}/change-password")]
    [EndpointSummary("Change my password")]
    [EndpointName("ChangePassword")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromRoute] int id, [FromBody] ChangePasswordResource resource,
        CancellationToken cancellationToken)
    {
        if (id != currentUser.CurrentUserId) return ForbiddenProblem();
        var result = await userCommandService.Handle(ChangePasswordCommandFromResourceAssembler.ToCommandFromResource(resource, id),
            cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }

    /// <summary>Assigns a role. The team always keeps at least one Administrator.</summary>
    [HttpPatch("{id:int}/role")]
    [RequireRole("ADMIN")]
    [EndpointSummary("Change the role of a member")]
    [EndpointName("ChangeUserRole")]
    public async Task<IActionResult> ChangeRole([FromRoute] int id, [FromBody] ChangeUserRoleResource resource,
        CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<UserRole>(resource.Role, out var role)) return ValidationRole();
        var restaurantId = currentUser.CurrentRestaurantId!.Value;
        var result = await userCommandService.Handle(new ChangeUserRoleCommand(restaurantId, id, role), cancellationToken);
        var restaurant = await externalRestaurantService.GetRestaurant(restaurantId, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            user => Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant, clock.Now)));
    }

    /// <summary>Unlocks a member locked by wrong passwords (Account Lock Service).</summary>
    [HttpPost("{id:int}/unlock")]
    [RequireRole("ADMIN")]
    [EndpointSummary("Unlock a team member")]
    [EndpointName("UnlockUser")]
    public async Task<IActionResult> Unlock([FromRoute] int id, CancellationToken cancellationToken)
    {
        var restaurantId = currentUser.CurrentRestaurantId!.Value;
        var result = await userCommandService.Handle(new UnlockUserCommand(restaurantId, id), cancellationToken);
        var restaurant = await externalRestaurantService.GetRestaurant(restaurantId, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            user => Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant, clock.Now)));
    }

    [HttpDelete("{id:int}")]
    [RequireRole("ADMIN")]
    [EndpointSummary("Remove a team member")]
    [EndpointName("RemoveMember")]
    public async Task<IActionResult> RemoveMember([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await userCommandService.Handle(
            new RemoveMemberCommand(currentUser.CurrentRestaurantId!.Value, id, currentUser.CurrentUserId!.Value), cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }

    private ObjectResult ForbiddenProblem() =>
        StatusCode(StatusCodes.Status403Forbidden, problemDetailsFactory.CreateProblemDetails(StatusCodes.Status403Forbidden,
            "Forbidden", "You can only see or change your own account."));

    private BadRequestObjectResult ValidationRole() =>
        BadRequest(problemDetailsFactory.CreateProblemDetails(StatusCodes.Status400BadRequest, "InvalidRole",
            "The role must be ADMIN or EMPLOYEE."));
}
