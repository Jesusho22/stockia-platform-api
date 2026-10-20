using System.Text.Json;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Turns any unhandled exception into an RFC 7807 ProblemDetails response,
///     so no stack trace or internal detail reaches the client.
/// </summary>
public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
            if (context.Response.HasStarted) throw;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "InternalServerError",
                Detail = "An unexpected error occurred. Please try again later.",
                Type = "https://httpstatuses.com/500"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
