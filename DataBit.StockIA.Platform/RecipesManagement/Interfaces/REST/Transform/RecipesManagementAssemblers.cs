using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.RecipesManagement.Application.Errors;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST.Transform;

public static class RecipeResourceFromEntityAssembler
{
    public static RecipeResource ToResourceFromEntity(Recipe recipe, decimal price) => new(
        recipe.Id, recipe.DishName, recipe.Active,
        recipe.Ingredients.Select(line => new RecipeIngredientResource(line.InventoryItemId, line.InventoryItemName,
            line.QuantityRequired, line.Unit)).ToList(),
        price);
}

public static class RecipeCommandFromResourceAssembler
{
    private static IReadOnlyList<RecipeIngredientData> Lines(SaveRecipeResource resource) =>
        resource.Ingredients.Select(line => new RecipeIngredientData(line.InventoryItemId, line.QuantityRequired)).ToList();

    public static CreateRecipeCommand ToCreateCommand(SaveRecipeResource resource, int restaurantId) =>
        new(restaurantId, resource.DishName, Lines(resource));

    public static UpdateRecipeCommand ToUpdateCommand(SaveRecipeResource resource, int id, int restaurantId) =>
        new(id, restaurantId, resource.DishName, Lines(resource));
}

public static class SaleResourceFromEntityAssembler
{
    public static SaleResource ToResourceFromEntity(Sale sale) => new(
        sale.Id, sale.SaleDate, sale.Channel.ToCode(), sale.Status.ToCode(),
        sale.LineItems.Select(line => new SaleLineItemResource(line.RecipeId, line.DishName, line.UnitPrice, line.Quantity)).ToList(),
        sale.Total);
}

/// <summary>
///     Maps RecipesManagementError to HTTP status codes. CannotSell → 422 with
///     details { dish, shortages[] } so the Web Application shows which supply
///     is missing or expired.
/// </summary>
public static class RecipesManagementActionResultAssembler
{
    public static int StatusCodeOf(Enum? error) => error switch
    {
        RecipesManagementError.RecipeNotFound => StatusCodes.Status404NotFound,
        RecipesManagementError.SaleNotFound => StatusCodes.Status404NotFound,
        RecipesManagementError.IngredientNotFound => StatusCodes.Status422UnprocessableEntity,
        RecipesManagementError.InvalidData => StatusCodes.Status400BadRequest,
        RecipesManagementError.CannotSell => StatusCodes.Status422UnprocessableEntity,
        RecipesManagementError.AlreadyVoided => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);

    public static IActionResult ToActionResult(Result result, ProblemDetailsFactory factory, Func<IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);
}
