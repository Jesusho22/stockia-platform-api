using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.StockManagement.Domain.Repositories;

public interface IStockConsumptionRepository : IBaseRepository<StockConsumption>
{
    Task<IReadOnlyList<StockConsumption>> FindAllByRestaurantIdAsync(int restaurantId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default);
}
