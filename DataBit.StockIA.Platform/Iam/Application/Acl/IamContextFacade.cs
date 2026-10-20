using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Iam.Interfaces.Acl;

namespace DataBit.StockIA.Platform.Iam.Application.Acl;

public class IamContextFacade(IRestaurantRepository restaurantRepository) : IIamContextFacade
{
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
