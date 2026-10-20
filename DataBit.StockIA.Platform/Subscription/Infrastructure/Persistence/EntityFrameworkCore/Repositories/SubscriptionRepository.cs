using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.Subscription.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SubscriptionRepository(AppDbContext context) : BaseRepository<RestaurantSubscription>(context), ISubscriptionRepository
{
    public Task<RestaurantSubscription?> FindByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        Context.Set<RestaurantSubscription>().FirstOrDefaultAsync(subscription => subscription.RestaurantId == restaurantId, cancellationToken);
}
