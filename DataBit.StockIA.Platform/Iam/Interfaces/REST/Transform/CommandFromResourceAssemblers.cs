using DataBit.StockIA.Platform.Iam.Domain.Model.Commands;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Transform;

public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommandFromResource(SignInResource resource) => new(resource.Email, resource.Password);
}

public static class PasswordResetCommandFromResourceAssembler
{
    public static RequestPasswordResetCommand ToRequestCommand(RequestPasswordResetResource resource) => new(resource.Email);

    public static ResetPasswordCommand ToResetCommand(ResetPasswordResource resource) =>
        new(resource.Email, resource.Code, resource.NewPassword);
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
