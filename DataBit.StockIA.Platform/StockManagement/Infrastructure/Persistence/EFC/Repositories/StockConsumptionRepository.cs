using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Repositories;

namespace DataBit.StockIA.Platform.StockManagement.Infrastructure.Persistence.EFC.Repositories;

public class StockConsumptionRepository(AppDbContext context)
    : BaseRepository<StockConsumption>(context), IStockConsumptionRepository
{
    public async Task<IReadOnlyList<StockConsumption>> FindAllByRestaurantIdAsync(int restaurantId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default) =>
        await Context.Set<StockConsumption>()
            .Where(consumption => consumption.RestaurantId == restaurantId && consumption.ConsumedOn >= from && consumption.ConsumedOn <= to)
            .ToListAsync(cancellationToken);
}
