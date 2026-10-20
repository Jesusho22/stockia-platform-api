using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.Subscription.Domain.Repositories;

public interface ISubscriptionRepository : IBaseRepository<RestaurantSubscription>
{
    Task<RestaurantSubscription?> FindByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
