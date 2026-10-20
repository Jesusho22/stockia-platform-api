using DataBit.StockIA.Platform.Alerts.Application.CommandServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Events;
using DataBit.StockIA.Platform.Shared.Application.Internal.EventHandlers;

namespace DataBit.StockIA.Platform.Alerts.Application.Internal.EventHandlers;

/// <summary>Re-evaluates the automatic alerts of the supply that just changed.</summary>
public class StockLevelChangedEventHandler(IAlertCommandService alertCommandService) : IEventHandler<StockLevelChangedEvent>
{
    public Task Handle(StockLevelChangedEvent domainEvent, CancellationToken cancellationToken) =>
        alertCommandService.Handle(new EvaluateInventoryAlertsCommand(domainEvent.RestaurantId, domainEvent.InventoryItemId), cancellationToken);
}
