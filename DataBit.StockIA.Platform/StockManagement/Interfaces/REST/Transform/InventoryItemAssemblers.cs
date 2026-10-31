using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.StockManagement.Application.Errors;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Interfaces.REST.Resources;

namespace DataBit.StockIA.Platform.StockManagement.Interfaces.REST.Transform;

public static class InventoryItemResourceFromEntityAssembler
{
    public static InventoryItemResource ToResourceFromEntity(InventoryItem item, DateOnly today) => new(
        item.Id, item.Name, item.Unit, item.Quantity, item.MinThreshold, item.StorageType.ToCode(), item.ShelfLifeDays,
        item.ExpirationDate.ToString("yyyy-MM-dd"), item.UnitCost, item.StockValue, item.StatusOn(today).ToCode());
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

/// <summary>Maps StockManagementError to HTTP status codes.</summary>
public static class StockManagementActionResultAssembler
{
    public static int StatusCodeOf(Enum? error) => error switch
    {
        StockManagementError.InventoryItemNotFound => StatusCodes.Status404NotFound,
        StockManagementError.InvalidData => StatusCodes.Status400BadRequest,
        StockManagementError.InsufficientStock => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);

    public static IActionResult ToActionResult(Result result, ProblemDetailsFactory factory, Func<IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);
}
