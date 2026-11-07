using DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Queries;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Repositories;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Application.Internal.QueryServices;

public class AlertQueryService(IAlertRepository alertRepository) : IAlertQueryService
{
    public Task<IReadOnlyList<Alert>> Handle(GetAlertsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        alertRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);

}