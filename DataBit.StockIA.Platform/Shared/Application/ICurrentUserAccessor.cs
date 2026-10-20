namespace DataBit.StockIA.Platform.Shared.Application;

/// <summary>
///     Identity of the authenticated request (user, restaurant and role), read
///     from the JWT validated by the IAM authorization middleware. Application
///     and interface code use it instead of touching HttpContext. Values are
///     null on anonymous endpoints (sign-in, sign-up, plans).
/// </summary>
public interface ICurrentUserAccessor
{
    int? CurrentUserId { get; }
    int? CurrentRestaurantId { get; }
    string? CurrentUserRole { get; }
    bool IsAdmin { get; }
}
