using DataBit.StockIA.Platform.Shared.Infrastructure.Pipeline.Middleware.Components;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>Registered first, so it wraps every other middleware.</summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
