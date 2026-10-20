using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;

public static class RequestAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestAuthorization(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestAuthorizationMiddleware>();
}
