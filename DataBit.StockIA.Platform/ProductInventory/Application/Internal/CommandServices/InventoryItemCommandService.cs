using Cortex.Mediator;
using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Errors;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Events;
using DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;
using DataBit.StockIA.Platform.ProductInventory.Domain.Services;
using DataBit.StockIA.Platform.ProductInventory.Resources;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.ProductInventory.Application.Internal.CommandServices;

/// <summary>
///     Supplies (lots) of the restaurant and RecipeStockDeductionService.
///     Every stock change publishes StockLevelChangedEvent for Alerts.
/// </summary>
public class InventoryItemCommandService(
    IInventoryItemRepository inventoryItemRepository,
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRestaurantClock clock,
    IMediator mediator,
    ILogger<InventoryItemCommandService> logger,
    IStringLocalizer<ProductInventoryMessages> localizer) : IInventoryItemCommandService
{
    public async Task<Result<InventoryItem>> Handle(CreateInventoryItemCommand command, CancellationToken cancellationToken)
    {
        InventoryItem item;
        try
        {
            item = new InventoryItem(command.RestaurantId, command.Name, command.Unit, command.Quantity, command.MinThreshold,
                command.StorageType, command.ShelfLifeDays, command.UnitCost, clock.Today);
        }
        catch (ArgumentException exception)
        {
            return Result<InventoryItem>.Failure(ProductInventoryError.InvalidData, exception.Message);
        }

        await inventoryItemRepository.AddAsync(item, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        await PublishStockChanged(item, cancellationToken);
        return Result<InventoryItem>.Success(item);
    }

    /// <summary>Saving a lot recalculates its expiration date from the shelf life.</summary>
    public async Task<Result<InventoryItem>> Handle(UpdateInventoryItemCommand command, CancellationToken cancellationToken)
    {
        var item = await inventoryItemRepository.FindByIdAsync(command.InventoryItemId, cancellationToken);
        if (item == null || item.RestaurantId != command.RestaurantId)
            return Result<InventoryItem>.Failure(ProductInventoryError.InventoryItemNotFound,
                localizer[nameof(ProductInventoryError.InventoryItemNotFound)]);
        try
        {
            item.Update(command.Name, command.Unit, command.Quantity, command.MinThreshold, command.StorageType,
                command.ShelfLifeDays, command.UnitCost, clock.Today);
        }
        catch (ArgumentException exception)
        {
            return Result<InventoryItem>.Failure(ProductInventoryError.InvalidData, exception.Message);
        }

        inventoryItemRepository.Update(item);
        await unitOfWork.CompleteAsync(cancellationToken);
        await PublishStockChanged(item, cancellationToken);
        return Result<InventoryItem>.Success(item);
    }

    public async Task<Result> Handle(DeleteInventoryItemCommand command, CancellationToken cancellationToken)
    {
        var item = await inventoryItemRepository.FindByIdAsync(command.InventoryItemId, cancellationToken);
        if (item == null || item.RestaurantId != command.RestaurantId)
            return Result.Failure(ProductInventoryError.InventoryItemNotFound, localizer[nameof(ProductInventoryError.InventoryItemNotFound)]);

        inventoryItemRepository.Remove(item);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    ///     Discounts the supplies of a sold dish: freshest usable lot first, never
    ///     an expired lot. Fails without changing anything when stock is short.
    /// </summary>
    public async Task<Result> Handle(DeductStockForRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(command.RecipeId, cancellationToken);
        if (recipe == null || recipe.RestaurantId != command.RestaurantId)
            return Result.Failure(ProductInventoryError.RecipeNotFound, localizer[nameof(ProductInventoryError.RecipeNotFound)]);

        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(command.RestaurantId, cancellationToken);
        var plan = StockAllocationService.Plan(recipe, items, command.Portions, clock.Today);
        if (!plan.CanFulfill)
            return Result.Failure(ProductInventoryError.InsufficientStock, localizer[nameof(ProductInventoryError.InsufficientStock)],
                plan.Shortages);

        foreach (var deduction in plan.Deductions) deduction.Item.Deduct(deduction.Taken);
        await unitOfWork.CompleteAsync(cancellationToken);
        foreach (var deduction in plan.Deductions) await PublishStockChanged(deduction.Item, cancellationToken);
        return Result.Success();
    }

    /// <summary>Alerts are a side effect: a failing handler never undoes the stock change.</summary>
    private async Task PublishStockChanged(InventoryItem item, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.PublishAsync(new StockLevelChangedEvent(item.RestaurantId, item.Id), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Stock alerts could not be evaluated for inventory item {Id}", item.Id);
        }
    }
}
