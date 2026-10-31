using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Application.Errors;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.Iam.Interfaces.REST.Transform;

/// <summary>Maps IamError to HTTP status codes.</summary>
public static class IamActionResultAssembler
{
    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);

    public static IActionResult ToActionResult(Result result, ProblemDetailsFactory factory, Func<IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);

    private static int StatusCodeOf(Enum? error) => error switch
    {
        IamError.InvalidCredentials => StatusCodes.Status401Unauthorized,
        IamError.AccountLocked => StatusCodes.Status423Locked,
        IamError.InvalidResetCode => StatusCodes.Status422UnprocessableEntity,
        IamError.CurrentPasswordInvalid => StatusCodes.Status422UnprocessableEntity,
        IamError.SamePassword => StatusCodes.Status422UnprocessableEntity,
        IamError.EmailAlreadyTaken => StatusCodes.Status409Conflict,
        IamError.LastAdministrator => StatusCodes.Status409Conflict,
        IamError.CannotRemoveYourself => StatusCodes.Status409Conflict,
        IamError.UserNotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError
    };
}
