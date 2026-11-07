using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Patterns;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;

public interface IAlertCommandService
{
    Task<Result<Alert>> Handle(CreateAlertCommand command, CancellationToken cancellationToken);
    Task<Result<Alert>> Handle(UpdateAlertCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteAlertCommand command, CancellationToken cancellationToken);
    Task<Result<Alert>> Handle(AcknowledgeAlertCommand command, CancellationToken cancellationToken);
    Task<Result<Alert>> Handle(RetryAlertDeliveryCommand command, CancellationToken cancellationToken);
    Task<int> Handle(EvaluateInventoryAlertsCommand command, CancellationToken cancellationToken);

}