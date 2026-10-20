using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.DemandForecasting.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class DemandForecastRepository(AppDbContext context) : BaseRepository<DemandForecast>(context), IDemandForecastRepository
{
    public override Task<DemandForecast?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Context.Set<DemandForecast>().FirstOrDefaultAsync(forecast => forecast.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DemandForecast>> FindRecentByRestaurantIdAsync(int restaurantId, int limit,
        CancellationToken cancellationToken = default) =>
        await Context.Set<DemandForecast>().Where(forecast => forecast.RestaurantId == restaurantId)
            .OrderByDescending(forecast => forecast.Id).Take(limit).ToListAsync(cancellationToken);

    public Task<DemandForecast?> FindLatestByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        Context.Set<DemandForecast>().Where(forecast => forecast.RestaurantId == restaurantId)
            .OrderByDescending(forecast => forecast.Id).FirstOrDefaultAsync(cancellationToken);
}
