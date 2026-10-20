using DataBit.StockIA.Platform.Alerts.Application.QueryServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Queries;
using DataBit.StockIA.Platform.Alerts.Domain.Repositories;

namespace DataBit.StockIA.Platform.Alerts.Application.Internal.QueryServices;

public class AlertQueryService(IAlertRepository alertRepository, IRecommendationRepository recommendationRepository) : IAlertQueryService
{
    public Task<IReadOnlyList<Alert>> Handle(GetAlertsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        alertRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);

    public Task<IReadOnlyList<Recommendation>> Handle(GetRecommendationsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        recommendationRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);
}
