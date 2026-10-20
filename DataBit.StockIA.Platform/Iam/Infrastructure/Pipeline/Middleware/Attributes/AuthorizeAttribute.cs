namespace DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Documents that the endpoint needs a valid bearer token (the middleware
///     requires one on every endpoint unless [AllowAnonymous] is present).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthorizeAttribute : Attribute
{
}
