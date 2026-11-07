using Cortex.Mediator;
using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Application.Errors;
using DataBit.StockIA.Platform.StockManagement.Application.Services;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Events;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Domain.Services;
using DataBit.StockIA.Platform.StockManagement.Resources;

namespace DataBit.StockIA.Platform.StockManagement.Application.Internal.CommandServices;

/// <summary>
///     Add, modify and remove supplies (C4: Add / Modify / Remove Supplies
///     Component) and take out what a sale consumes. Every stock change
///     publishes StockLevelChangedEvent for Notifications &amp; Messaging.
/// </summary>
public class InventoryItemCommandService(
    IInventoryItemRepository inventoryItemRepository,
    IStockConsumptionRepository stockConsumptionRepository,
    IUnitOfWork unitOfWork,
    IRestaurantClock clock,
    IMediator mediator,
    ILogger<InventoryItemCommandService> logger,
    IStringLocalizer<StockManagementMessages> localizer) : IInventoryItemCommandService
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
            return Result<InventoryItem>.Failure(StockManagementError.InvalidData, exception.Message);
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
            return Result<InventoryItem>.Failure(StockManagementError.InventoryItemNotFound,
                localizer[nameof(StockManagementError.InventoryItemNotFound)]);
        try
        {
            item.Update(command.Name, command.Unit, command.Quantity, command.MinThreshold, command.StorageType,
                command.ShelfLifeDays, command.UnitCost, clock.Today);
        }
        catch (ArgumentException exception)
        {
            return Result<InventoryItem>.Failure(StockManagementError.InvalidData, exception.Message);
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
            return Result.Failure(StockManagementError.InventoryItemNotFound,
                localizer[nameof(StockManagementError.InventoryItemNotFound)]);

        inventoryItemRepository.Remove(item);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    ///     Takes out what a sold dish consumes: freshest usable lot first, never an
    ///     expired lot. Fails without changing anything when stock is short, and
    ///     records each lot used as a StockConsumption.
    /// </summary>
    public async Task<Result> Handle(DeductStockCommand command, CancellationToken cancellationToken)
    {
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(command.RestaurantId, cancellationToken);
        var plan = StockAllocationService.Plan(command.Requirements, items, command.Portions, clock.Today);
        if (!plan.CanFulfill)
            return Result.Failure(StockManagementError.InsufficientStock, localizer[nameof(StockManagementError.InsufficientStock)],
                plan.Shortages);

        foreach (var deduction in plan.Deductions)
        {
            await stockConsumptionRepository.AddAsync(new StockConsumption(command.RestaurantId, deduction.Item, deduction.Taken, clock.Today),
                cancellationToken);
            deduction.Item.Deduct(deduction.Taken);
        }

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
