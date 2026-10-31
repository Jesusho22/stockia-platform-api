using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace DataBit.StockIA.Platform.MlRecommendations.Infrastructure.Persistence.EFC.Repositories;

public class RecommendationRepository(AppDbContext context) : BaseRepository<Recommendation>(context), IRecommendationRepository
{
    public async Task<IReadOnlyList<Recommendation>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await Context.Set<Recommendation>().Where(item => item.RestaurantId == restaurantId).OrderByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Recommendation>> FindPendingByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await Context.Set<Recommendation>().Where(item => item.RestaurantId == restaurantId && !item.Applied).ToListAsync(cancellationToken);
}
