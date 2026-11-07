using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;

public interface IAlertQueryService
{
    Task<IReadOnlyList<Alert>> Handle(GetAlertsByRestaurantIdQuery query, CancellationToken cancellationToken);
}