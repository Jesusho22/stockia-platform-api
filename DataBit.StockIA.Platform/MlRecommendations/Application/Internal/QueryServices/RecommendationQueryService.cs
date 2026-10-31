using DataBit.StockIA.Platform.MlRecommendations.Application.Services;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Queries;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Repositories;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Internal.QueryServices;

public class RecommendationQueryService(IRecommendationRepository recommendationRepository) : IRecommendationQueryService
{
    public Task<IReadOnlyList<Recommendation>> Handle(GetRecommendationsByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        recommendationRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);
}
