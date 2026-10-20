using Microsoft.AspNetCore.Mvc;

namespace DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;

/// <summary>
///     Builds RFC 7807 responses the same way in every bounded context. The
///     title is the error code (e.g. "EmailAlreadyTaken") the Web Application
///     maps to its own i18n messages; the detail is a localized text.
/// </summary>
public class ProblemDetailsFactory
{
    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(int statusCode, string title, string detail,
        string? instance = null, object? details = null)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
        if (details != null) problem.Extensions["details"] = details;
        return problem;
    }

    public ActionResult ToActionResult(int statusCode, string title, string detail, object? details = null)
    {
        var problemDetails = CreateProblemDetails(statusCode, title, detail, details: details);
        return new ObjectResult(problemDetails) { StatusCode = statusCode, ContentTypes = { "application/problem+json" } };
    }
}
