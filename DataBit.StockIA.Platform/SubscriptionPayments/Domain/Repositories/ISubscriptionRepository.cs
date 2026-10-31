using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

public interface ISubscriptionRepository : IBaseRepository<RestaurantSubscription>
{
    Task<RestaurantSubscription?> FindByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
