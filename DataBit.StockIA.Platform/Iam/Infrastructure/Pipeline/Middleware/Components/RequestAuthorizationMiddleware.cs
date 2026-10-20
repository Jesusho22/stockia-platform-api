using System.Security.Claims;
using System.Text.Json;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Validates the bearer token of every request unless the endpoint has
///     [AllowAnonymous], then checks [RequireRole]. The token claims are set on
///     HttpContext.User, where Shared's ICurrentUserAccessor reads them.
/// </summary>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        var metadata = context.GetEndpoint()?.Metadata;
        if (metadata?.Any(item => item is AllowAnonymousAttribute) ?? false)
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.FirstOrDefault();
        var token = header?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true ? header[7..].Trim() : null;
        var principal = token != null ? tokenService.ValidateToken(token) : null;
        if (principal == null)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized",
                "A valid bearer token is required. Sign in again.");
            return;
        }

        context.User = principal;

        var requiredRoles = metadata?.OfType<RequireRoleAttribute>().SelectMany(attribute => attribute.Roles).ToHashSet();
        var role = principal.FindFirst(ClaimTypes.Role)?.Value;
        if (requiredRoles is { Count: > 0 } && (role == null || !requiredRoles.Contains(role)))
        {
            await WriteProblem(context, StatusCodes.Status403Forbidden, "Forbidden",
                "Your role does not have access to this resource.");
            return;
        }

        await next(context);
    }

    private static async Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail
        }));
    }
}
