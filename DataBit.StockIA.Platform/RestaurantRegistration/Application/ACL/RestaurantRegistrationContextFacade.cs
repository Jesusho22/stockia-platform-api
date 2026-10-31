using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Repositories;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.ACL;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.ACL;

public class RestaurantRegistrationContextFacade(IRestaurantRepository restaurantRepository) : IRestaurantRegistrationContextFacade
{
    public async Task<RestaurantInfo?> GetRestaurant(int restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await restaurantRepository.FindByIdAsync(restaurantId, cancellationToken);
        return restaurant == null
            ? null
            : new RestaurantInfo(restaurant.Id, restaurant.Name, restaurant.Address, restaurant.Phone, restaurant.Latitude,
                restaurant.Longitude);
    }

    public async Task<(double latitude, double longitude)?> GetRestaurantLocation(int restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await restaurantRepository.FindByIdAsync(restaurantId, cancellationToken);
        return restaurant == null ? null : (restaurant.Latitude, restaurant.Longitude);
    }

    public async Task<IReadOnlyCollection<int>> GetRestaurantIds(CancellationToken cancellationToken)
    {
        var restaurants = await restaurantRepository.ListAsync(cancellationToken);
        return restaurants.Select(restaurant => restaurant.Id).ToList();
    }
}
