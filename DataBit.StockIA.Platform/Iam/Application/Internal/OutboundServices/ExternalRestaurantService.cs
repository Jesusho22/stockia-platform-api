using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.ACL;

namespace DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;

/// <summary>Anti-corruption layer towards Restaurant Registration (restaurant data shown with the user).</summary>
public class ExternalRestaurantService(IRestaurantRegistrationContextFacade restaurantRegistrationContextFacade)
{
    public Task<RestaurantInfo?> GetRestaurant(int restaurantId, CancellationToken cancellationToken) =>
        restaurantRegistrationContextFacade.GetRestaurant(restaurantId, cancellationToken);
}
