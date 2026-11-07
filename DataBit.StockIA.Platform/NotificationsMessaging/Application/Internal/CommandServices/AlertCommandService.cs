using DataBit.StockIA.Platform.NotificationsMessaging.Application.Errors;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Commands;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Repositories;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Resources;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;
using Microsoft.Extensions.Localization;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.CommandServices;

public class AlertCommandService(
    IAlertRepository alertRepository,
    INotificationSender notificationSender,
    IStockManagementContextFacade stockManagementContextFacade,
    IRestaurantClock clock,
    IUnitOfWork unitOfWork,
    IStringLocalizer<NotificationsMessagingMessages> localizer) : IAlertCommandService
{
    /// <summary>A manual alert; its main channel counts as the first delivery attempt.</summary>
    public async Task<Result<Alert>> Handle(CreateAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = new Alert(command.RestaurantId, command.Type, command.Severity, command.Message, command.Channel, clock.Now.ToUniversalTime());
        if (await notificationSender.SendAsync(command.RestaurantId, command.Channel, alert.Message, cancellationToken))
            alert.RegisterDelivery(command.Channel);
        await alertRepository.AddAsync(alert, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Alert>.Success(alert);
    }

    public async Task<Result<Alert>> Handle(UpdateAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = await FindAsync(command.AlertId, command.RestaurantId, cancellationToken);
        if (alert == null) return NotFound();
        alert.Update(command.Type, command.Severity, command.Message, command.Channel);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Alert>.Success(alert);
    }

    public async Task<Result> Handle(DeleteAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = await FindAsync(command.AlertId, command.RestaurantId, cancellationToken);
        if (alert == null) return Result.Failure(NotificationsMessagingError.AlertNotFound, localizer[nameof(NotificationsMessagingError.AlertNotFound)]);
        alertRepository.Remove(alert);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Only delivered alerts (every required channel) can be marked as attended.</summary>
    public async Task<Result<Alert>> Handle(AcknowledgeAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = await FindAsync(command.AlertId, command.RestaurantId, cancellationToken);
        if (alert == null) return NotFound();
        if (!alert.Acknowledge()) return Result<Alert>.Failure(NotificationsMessagingError.NotDelivered, localizer[nameof(NotificationsMessagingError.NotDelivered)]);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Alert>.Success(alert);
    }

    public async Task<Result<Alert>> Handle(RetryAlertDeliveryCommand command, CancellationToken cancellationToken)
    {
        var alert = await FindAsync(command.AlertId, command.RestaurantId, cancellationToken);
        if (alert == null) return NotFound();
        if (alert.PendingChannel is not { } channel)
            return Result<Alert>.Failure(NotificationsMessagingError.AlreadyDelivered, localizer[nameof(NotificationsMessagingError.AlreadyDelivered)]);
        if (!await notificationSender.SendAsync(alert.RestaurantId, channel, alert.Message, cancellationToken))
            return Result<Alert>.Failure(NotificationsMessagingError.DeliveryFailed, localizer[nameof(NotificationsMessagingError.DeliveryFailed)]);
        alert.RegisterDelivery(channel);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Alert>.Success(alert);
    }

    /// <summary>
    ///     Automatic alerts (InventoryAlertPolicy). An open alert of the same
    ///     supply and type is refreshed instead of repeated; new ones are sent
    ///     by their main channel (critical ones by WhatsApp and email).
    /// </summary>
    public async Task<int> Handle(EvaluateInventoryAlertsCommand command, CancellationToken cancellationToken)
    {
        IReadOnlyList<InventoryLotInfo> lots;
        if (command.InventoryItemId is { } id)
        {
            var single = await stockManagementContextFacade.GetInventoryLot(command.RestaurantId, id, cancellationToken);
            lots = single == null ? [] : [single];
        }
        else
        {
            lots = await stockManagementContextFacade.GetInventory(command.RestaurantId, cancellationToken);
        }
        var created = 0;
        foreach (var lot in lots)
        {
            var snapshot = new StockSnapshot(lot.InventoryItemId, lot.Name, lot.Unit, lot.Quantity, lot.MinThreshold, lot.DaysToExpire, lot.Status);
            foreach (var proposal in InventoryAlertPolicy.Evaluate(snapshot))
            {
                var open = await alertRepository.FindOpenAsync(command.RestaurantId, lot.InventoryItemId, proposal.Type, cancellationToken);
                if (open != null)
                {
                    open.Update(proposal.Type, proposal.Severity, proposal.Message, proposal.Channel);
                    continue;
                }

                var alert = new Alert(command.RestaurantId, proposal.Type, proposal.Severity, proposal.Message, proposal.Channel,
                    clock.Now.ToUniversalTime(), lot.InventoryItemId);
                foreach (var channel in alert.RequiredChannels)
                    if (await notificationSender.SendAsync(command.RestaurantId, channel, alert.Message, cancellationToken))
                        alert.RegisterDelivery(channel);
                await alertRepository.AddAsync(alert, cancellationToken);
                created++;
            }
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return created;
    }

    private async Task<Alert?> FindAsync(int alertId, int restaurantId, CancellationToken cancellationToken)
    {
        var alert = await alertRepository.FindByIdAsync(alertId, cancellationToken);
        return alert?.RestaurantId == restaurantId ? alert : null;
    }

    private Result<Alert> NotFound() => Result<Alert>.Failure(NotificationsMessagingError.AlertNotFound, localizer[nameof(NotificationsMessagingError.AlertNotFound)]);
}