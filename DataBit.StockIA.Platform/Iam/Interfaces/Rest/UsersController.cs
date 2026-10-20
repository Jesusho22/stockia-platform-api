using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Application.CommandServices;
using DataBit.StockIA.Platform.Iam.Application.QueryServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.Queries;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest;

/// <summary>
///     Team of the restaurant (Roles &amp; permissions) and the profile of the
///     signed-in user. Listing and changing the team is for Administrators;
///     every user edits only their own profile and password.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/users")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Team members and profile")]
public class UsersController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService,
    IRestaurantQueryService restaurantQueryService,
    ICurrentUserAccessor currentUser,
    IConfiguration configuration,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [RequireRole("ADMIN")]
    [SwaggerOperation(Summary = "List the team of the restaurant (newest first)", OperationId = "GetUsers")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var restaurantId = currentUser.CurrentRestaurantId!.Value;
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(restaurantId), cancellationToken);
        var users = await userQueryService.Handle(new GetUsersByRestaurantIdQuery(restaurantId), cancellationToken);
        return Ok(users.Select(user => UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant)));
    }

    [HttpGet("me")]
    [SwaggerOperation(Summary = "Get the signed-in user", OperationId = "GetCurrentUser")]
    public Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken) =>
        GetUserById(currentUser.CurrentUserId!.Value, cancellationToken);

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a team member", OperationId = "GetUserById")]
    public async Task<IActionResult> GetUserById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (user == null || user.RestaurantId != currentUser.CurrentRestaurantId) return NotFound();
        if (!currentUser.IsAdmin && user.Id != currentUser.CurrentUserId) return ForbiddenProblem();
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(user.RestaurantId), cancellationToken);
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant));
    }

    /// <summary>Invites a member with a temporary password (a real deployment would email it).</summary>
    [HttpPost]
    [RequireRole("ADMIN")]
    [SwaggerOperation(Summary = "Invite a team member", OperationId = "InviteMember")]
    [SwaggerResponse(StatusCodes.Status201Created, "Member created", typeof(UserResource))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email already registered")]
    public async Task<IActionResult> InviteMember([FromBody] InviteMemberResource resource, CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<UserRole>(resource.Role, out var role)) return ValidationRole();
        var restaurantId = currentUser.CurrentRestaurantId!.Value;
        var command = InviteMemberCommandFromResourceAssembler.ToCommandFromResource(resource, restaurantId, role,
            configuration["Iam:InvitationTemporaryPassword"] ?? "stockia123");
        var result = await userCommandService.Handle(command, cancellationToken);
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(restaurantId), cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, user =>
            CreatedAtAction(nameof(GetUserById), new { id = user.Id }, UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant)));
    }

    /// <summary>Profile data (name and email) of the signed-in user.</summary>
    [HttpPatch("{id:int}")]
    [SwaggerOperation(Summary = "Update my profile", OperationId = "UpdateUserProfile")]
    public async Task<IActionResult> UpdateProfile([FromRoute] int id, [FromBody] UpdateUserProfileResource resource,
        CancellationToken cancellationToken)
    {
        if (id != currentUser.CurrentUserId) return ForbiddenProblem();
        var result = await userCommandService.Handle(UpdateUserProfileCommandFromResourceAssembler.ToCommandFromResource(resource, id),
            cancellationToken);
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            user => Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant)));
    }

    /// <summary>Changes my password after verifying the current one.</summary>
    [HttpPost("{id:int}/change-password")]
    [SwaggerOperation(Summary = "Change my password", OperationId = "ChangePassword")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Password changed")]
    [SwaggerResponse(StatusCodes.Status422UnprocessableEntity, "The current password is not correct")]
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
    [SwaggerOperation(Summary = "Change the role of a member", OperationId = "ChangeUserRole")]
    public async Task<IActionResult> ChangeRole([FromRoute] int id, [FromBody] ChangeUserRoleResource resource,
        CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<UserRole>(resource.Role, out var role)) return ValidationRole();
        var restaurantId = currentUser.CurrentRestaurantId!.Value;
        var result = await userCommandService.Handle(new ChangeUserRoleCommand(restaurantId, id, role), cancellationToken);
        var restaurant = await restaurantQueryService.Handle(new GetRestaurantByIdQuery(restaurantId), cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            user => Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user, restaurant)));
    }

    [HttpDelete("{id:int}")]
    [RequireRole("ADMIN")]
    [SwaggerOperation(Summary = "Remove a team member", OperationId = "RemoveMember")]
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
