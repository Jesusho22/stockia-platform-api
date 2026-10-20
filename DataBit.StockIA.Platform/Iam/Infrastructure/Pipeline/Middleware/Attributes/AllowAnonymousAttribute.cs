namespace DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Endpoint reachable without a bearer token (checked by RequestAuthorizationMiddleware).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AllowAnonymousAttribute : Attribute
{
}
