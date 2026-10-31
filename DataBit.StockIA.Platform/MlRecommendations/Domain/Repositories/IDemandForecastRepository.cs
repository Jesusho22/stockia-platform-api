using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.MlRecommendations.Domain.Repositories;

public interface IDemandForecastRepository : IBaseRepository<DemandForecast>
{
    Task<IReadOnlyList<DemandForecast>> FindRecentByRestaurantIdAsync(int restaurantId, int limit, CancellationToken cancellationToken = default);
    Task<DemandForecast?> FindLatestByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
