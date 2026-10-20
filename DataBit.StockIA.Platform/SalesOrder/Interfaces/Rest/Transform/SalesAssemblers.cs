using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Errors;
using DataBit.StockIA.Platform.SalesOrder.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace DataBit.StockIA.Platform.SalesOrder.Interfaces.Rest.Transform;

public static class SaleResourceFromEntityAssembler
{
    public static SaleResource ToResourceFromEntity(Sale sale) => new(
        sale.Id, sale.SaleDate, sale.Channel.ToCode(), sale.Status.ToCode(),
        sale.LineItems.Select(line => new SaleLineItemResource(line.RecipeId, line.DishName, line.UnitPrice, line.Quantity)).ToList(),
        sale.Total);
}

public static class SalesActionResultAssembler
{
    /// <summary>
    ///     CannotSell → 422 with details { dish, shortages[] } so the Web
    ///     Application shows which supply is missing or expired.
    /// </summary>
    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, error => error switch
        {
            SalesError.RecipeNotFound => StatusCodes.Status404NotFound,
            SalesError.SaleNotFound => StatusCodes.Status404NotFound,
            SalesError.CannotSell => StatusCodes.Status422UnprocessableEntity,
            SalesError.AlreadyVoided => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        }, onSuccess);
}
