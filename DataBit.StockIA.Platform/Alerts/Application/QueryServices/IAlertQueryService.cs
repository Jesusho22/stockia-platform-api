using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.Alerts.Application.QueryServices;

public interface IAlertQueryService
{
    Task<IReadOnlyList<Alert>> Handle(GetAlertsByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Recommendation>> Handle(GetRecommendationsByRestaurantIdQuery query, CancellationToken cancellationToken);
}
