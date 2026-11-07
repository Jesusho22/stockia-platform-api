using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Persistence.EFC.Repositories;

public class SubscriptionRepository(AppDbContext context) : BaseRepository<RestaurantSubscription>(context), ISubscriptionRepository
{
    public Task<RestaurantSubscription?> FindByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        Context.Set<RestaurantSubscription>().FirstOrDefaultAsync(subscription => subscription.RestaurantId == restaurantId, cancellationToken);
}