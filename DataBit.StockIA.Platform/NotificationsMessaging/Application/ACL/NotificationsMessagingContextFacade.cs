using DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Commands;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Repositories;
using DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.ACL;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.ACL;

public class NotificationsMessagingContextFacade (
    IAlertRepository alertRepository,
    IAlertCommandService alertCommandService,
    INotificationSender notificationSender) : INotificationsMessagingContextFacade
{
public async Task<int> CountPendingAlerts(int restaurantId, CancellationToken cancellationToken) =>
    (await alertRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken)).Count(alert => !alert.Acknowledged);

public Task EvaluateInventoryAlerts(int restaurantId, CancellationToken cancellationToken) =>
    alertCommandService.Handle(new EvaluateInventoryAlertsCommand(restaurantId, null), cancellationToken);

public Task<bool> SendEmail(int restaurantId, string to, string subject, string body, CancellationToken cancellationToken) =>
    notificationSender.SendEmailAsync(restaurantId, to, subject, body, cancellationToken);

public bool IsEmailSimulated => notificationSender.IsSimulated;
}