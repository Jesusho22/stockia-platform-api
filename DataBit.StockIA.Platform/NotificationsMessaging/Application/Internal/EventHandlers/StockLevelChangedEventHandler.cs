using DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Internal.EventHandlers;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Events;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.EventHandlers;

/// <summary>Re-evaluates the automatic alerts of the supply that just changed.</summary>
public class StockLevelChangedEventHandler(IAlertCommandService alertCommandService) : IEventHandler<StockLevelChangedEvent>
{
    public Task Handle(StockLevelChangedEvent domainEvent, CancellationToken cancellationToken) =>
        alertCommandService.Handle(new EvaluateInventoryAlertsCommand(domainEvent.RestaurantId, domainEvent.InventoryItemId), cancellationToken);
}
