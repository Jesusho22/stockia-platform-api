using DataBit.StockIA.Platform.Alerts.Application.CommandServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;
using DataBit.StockIA.Platform.Alerts.Domain.Repositories;
using DataBit.StockIA.Platform.Alerts.Interfaces.Acl;

namespace DataBit.StockIA.Platform.Alerts.Application.Acl;

public class AlertsContextFacade(IAlertRepository alertRepository, IAlertCommandService alertCommandService) : IAlertsContextFacade
{
    public async Task<int> CountPendingAlerts(int restaurantId, CancellationToken cancellationToken) =>
        (await alertRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken)).Count(alert => !alert.Acknowledged);

    public Task EvaluateInventoryAlerts(int restaurantId, CancellationToken cancellationToken) =>
        alertCommandService.Handle(new EvaluateInventoryAlertsCommand(restaurantId, null), cancellationToken);
}
