using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.ACL;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Transform;

public static class UserResourceFromEntityAssembler
{
    public static UserResource ToResourceFromEntity(User user, RestaurantInfo? restaurant, DateTimeOffset now) => new(
        user.Id, user.FullName, user.Email, user.Role.ToCode(), user.RestaurantId,
        restaurant?.Name ?? string.Empty, restaurant?.Address ?? string.Empty, restaurant?.Phone ?? string.Empty,
        user.IsLockedAt(now), user.IsLockedAt(now) ? user.LockedUntil : null);

    public static AuthenticatedUserResource ToAuthenticatedResource(User user, RestaurantInfo? restaurant, string token) => new(
        user.Id, user.FullName, user.Email, user.Role.ToCode(), user.RestaurantId,
        restaurant?.Name ?? string.Empty, restaurant?.Address ?? string.Empty, restaurant?.Phone ?? string.Empty, token);
}

public static class PasswordResetRequestResourceFromValueAssembler
{
    public static PasswordResetRequestResource ToResource(PasswordResetRequest request, string message) => new(message,
        request.MaskedEmail,
        request.SimulatedEmail is { } email
            ? new SimulatedEmailResource(email.To, email.Subject, email.ResetLink, email.Code, email.ExpiresAt)
            : null);
}
