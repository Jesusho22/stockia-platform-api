using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Errors;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.ProductInventory.Domain.Services;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Transform;

public static class InventoryItemResourceFromEntityAssembler
{
    public static InventoryItemResource ToResourceFromEntity(InventoryItem item, DateOnly today) => new(
        item.Id, item.Name, item.Unit, item.Quantity, item.MinThreshold, item.StorageType.ToCode(), item.ShelfLifeDays,
        item.ExpirationDate.ToString("yyyy-MM-dd"), item.UnitCost, item.StockValue, item.StatusOn(today).ToCode());
}

public static class RecipeResourceFromEntityAssembler
{
    public static RecipeResource ToResourceFromEntity(Recipe recipe, IReadOnlyCollection<InventoryItem> items, DateOnly today) => new(
        recipe.Id, recipe.DishName, recipe.Active,
        recipe.Ingredients.Select(line => new RecipeIngredientResource(line.InventoryItemId, line.InventoryItemName,
            line.QuantityRequired, line.Unit)).ToList(),
        StockAllocationService.RecipePrice(recipe, items, today));
}

public static class InventoryItemCommandFromResourceAssembler
{
    public static CreateInventoryItemCommand ToCreateCommand(SaveInventoryItemResource resource, StorageType storageType, int restaurantId) =>
        new(restaurantId, resource.Name, resource.Unit, resource.Quantity, resource.MinThreshold, storageType, resource.ShelfLifeDays,
            resource.UnitCost);

    public static UpdateInventoryItemCommand ToUpdateCommand(SaveInventoryItemResource resource, StorageType storageType, int id,
        int restaurantId) =>
        new(id, restaurantId, resource.Name, resource.Unit, resource.Quantity, resource.MinThreshold, storageType,
            resource.ShelfLifeDays, resource.UnitCost);
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

public static class ProductInventoryActionResultAssembler
{
    public static int StatusCodeOf(Enum? error) => error switch
    {
        ProductInventoryError.InventoryItemNotFound => StatusCodes.Status404NotFound,
        ProductInventoryError.RecipeNotFound => StatusCodes.Status404NotFound,
        ProductInventoryError.IngredientNotFound => StatusCodes.Status422UnprocessableEntity,
        ProductInventoryError.InvalidData => StatusCodes.Status400BadRequest,
        ProductInventoryError.InsufficientStock => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);

    public static IActionResult ToActionResult(Result result, ProblemDetailsFactory factory, Func<IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);
}
