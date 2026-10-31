using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Services;

public interface IRecommendationQueryService
{
    Task<IReadOnlyList<Recommendation>> Handle(GetRecommendationsByRestaurantIdQuery query, CancellationToken cancellationToken);
}
