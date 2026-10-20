using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.Alerts.Domain.Repositories;

public interface IRecommendationRepository : IBaseRepository<Recommendation>
{
    Task<IReadOnlyList<Recommendation>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Recommendation>> FindPendingByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
