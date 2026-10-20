using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SalesOrder.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.SalesOrder.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SaleRepository(AppDbContext context) : BaseRepository<Sale>(context), ISaleRepository
{
    public override Task<Sale?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Context.Set<Sale>().FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sale>> FindAllByRestaurantIdAsync(int restaurantId, DateTimeOffset? from = null,
        DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Sale>().Where(sale => sale.RestaurantId == restaurantId);
        // Sale dates are stored in UTC.
        if (from is { } start)
        {
            var fromUtc = start.ToUniversalTime();
            query = query.Where(sale => sale.SaleDate >= fromUtc);
        }

        if (to is { } end)
        {
            var toUtc = end.ToUniversalTime();
            query = query.Where(sale => sale.SaleDate <= toUtc);
        }
        return await query.OrderByDescending(sale => sale.SaleDate).ThenByDescending(sale => sale.Id).ToListAsync(cancellationToken);
    }
}
