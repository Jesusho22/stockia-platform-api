namespace DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Only users with one of these roles may call the endpoint, e.g.
///     [RequireRole("ADMIN")] on Roles &amp; permissions, Recommendations and
///     the subscription. Other roles receive 403 Forbidden.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireRoleAttribute(params string[] roles) : Attribute
{
    public IReadOnlyCollection<string> Roles { get; } = roles;
}
