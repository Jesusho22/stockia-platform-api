using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Transform;

public static class UserResourceFromEntityAssembler
{
    public static UserResource ToResourceFromEntity(User user, Restaurant? restaurant) => new(
        user.Id, user.FullName, user.Email, user.Role.ToCode(), user.RestaurantId,
        restaurant?.Name ?? string.Empty, restaurant?.Address ?? string.Empty, restaurant?.Phone ?? string.Empty);

    public static AuthenticatedUserResource ToAuthenticatedResource(User user, Restaurant? restaurant, string token) => new(
        user.Id, user.FullName, user.Email, user.Role.ToCode(), user.RestaurantId,
        restaurant?.Name ?? string.Empty, restaurant?.Address ?? string.Empty, restaurant?.Phone ?? string.Empty, token);
}
