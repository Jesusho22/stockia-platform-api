using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Repositories;

namespace DataBit.StockIA.Platform.StockManagement.Infrastructure.Persistence.EFC.Repositories;

public class InventoryItemRepository(AppDbContext context) : BaseRepository<InventoryItem>(context), IInventoryItemRepository
{
    public async Task<IReadOnlyList<InventoryItem>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await Context.Set<InventoryItem>().Where(item => item.RestaurantId == restaurantId).OrderByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
}
