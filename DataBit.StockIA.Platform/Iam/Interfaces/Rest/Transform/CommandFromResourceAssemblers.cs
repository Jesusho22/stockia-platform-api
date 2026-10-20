using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Transform;

public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommandFromResource(SignInResource resource) => new(resource.Email, resource.Password);
}

public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommandFromResource(SignUpResource resource) =>
        new(resource.FullName, resource.RestaurantName, resource.Email, resource.Password);
}

public static class InviteMemberCommandFromResourceAssembler
{
    public static InviteMemberCommand ToCommandFromResource(InviteMemberResource resource, int restaurantId, UserRole role,
        string defaultPassword) =>
        new(restaurantId, resource.FullName, resource.Email, role, resource.TemporaryPassword ?? defaultPassword);
}

public static class UpdateUserProfileCommandFromResourceAssembler
{
    public static UpdateUserProfileCommand ToCommandFromResource(UpdateUserProfileResource resource, int userId) =>
        new(userId, resource.FullName, resource.Email);
}

public static class ChangePasswordCommandFromResourceAssembler
{
    public static ChangePasswordCommand ToCommandFromResource(ChangePasswordResource resource, int userId) =>
        new(userId, resource.CurrentPassword, resource.NewPassword);
}

public static class UpdateRestaurantCommandFromResourceAssembler
{
    public static UpdateRestaurantCommand ToCommandFromResource(UpdateRestaurantResource resource, int restaurantId) =>
        new(restaurantId, resource.Name, resource.Address ?? string.Empty, resource.Phone ?? string.Empty);
}
