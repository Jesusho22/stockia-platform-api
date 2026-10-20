using System.Security.Claims;
using DataBit.StockIA.Platform.Shared.Application;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Security;

/// <summary>
///     Reads user, restaurant and role from the claims set by the IAM
///     authorization middleware. The claim names are the contract with IAM's
///     TokenService (kept as strings so Shared does not depend on IAM).
/// </summary>
public class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public const string RestaurantIdClaimType = "restaurant_id";
    public const string AdminRole = "ADMIN";

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? CurrentUserId => ParseInt(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    public int? CurrentRestaurantId => ParseInt(User?.FindFirst(RestaurantIdClaimType)?.Value);

    public string? CurrentUserRole => User?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAdmin => CurrentUserRole == AdminRole;

    private static int? ParseInt(string? value) => int.TryParse(value, out var id) ? id : null;
}
