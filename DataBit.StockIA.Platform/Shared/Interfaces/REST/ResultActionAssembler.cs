using Microsoft.AspNetCore.Mvc;
using ProblemDetailsFactory = DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails.ProblemDetailsFactory;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.Shared.Interfaces.REST;

/// <summary>
///     Translates a Result into an IActionResult. Each bounded context passes
///     the function that maps its error enum to an HTTP status code.
/// </summary>
public static class ResultActionAssembler
{
    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory,
        Func<Enum?, int> statusCodeOf, Func<T, IActionResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess(result.Value!)
            : factory.ToActionResult(statusCodeOf(result.Error), result.Error?.ToString() ?? "InternalServerError",
                result.Message, result.Details);
    }

    public static IActionResult ToActionResult(Result result, ProblemDetailsFactory factory, Func<Enum?, int> statusCodeOf,
        Func<IActionResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess()
            : factory.ToActionResult(statusCodeOf(result.Error), result.Error?.ToString() ?? "InternalServerError",
                result.Message, result.Details);
    }
}
