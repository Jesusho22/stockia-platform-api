using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Domain.Model.Errors;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Transform;

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
        IamError.CurrentPasswordInvalid => StatusCodes.Status422UnprocessableEntity,
        IamError.SamePassword => StatusCodes.Status422UnprocessableEntity,
        IamError.EmailAlreadyTaken => StatusCodes.Status409Conflict,
        IamError.LastAdministrator => StatusCodes.Status409Conflict,
        IamError.CannotRemoveYourself => StatusCodes.Status409Conflict,
        IamError.UserNotFound => StatusCodes.Status404NotFound,
        IamError.RestaurantNotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError
    };
}
