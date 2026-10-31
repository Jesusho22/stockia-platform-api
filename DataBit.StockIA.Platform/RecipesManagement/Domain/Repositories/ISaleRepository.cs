using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;

public interface ISaleRepository : IBaseRepository<Sale>
{
    /// <summary>Newest first. from/to filter by SaleDate (inclusive, UTC instants).</summary>
    Task<IReadOnlyList<Sale>> FindAllByRestaurantIdAsync(int restaurantId, DateTimeOffset? from = null, DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);
}
