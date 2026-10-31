namespace DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.ACL;

/// <summary>The restaurant as other bounded contexts see it.</summary>
public record RestaurantInfo(int RestaurantId, string Name, string Address, string Phone, double Latitude, double Longitude);

/// <summary>What other bounded contexts may read from Restaurant Registration.</summary>
public interface IRestaurantRegistrationContextFacade
{
    Task<RestaurantInfo?> GetRestaurant(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Coordinates of the restaurant for the weather of the demand forecast.</summary>
    Task<(double latitude, double longitude)?> GetRestaurantLocation(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Every restaurant id (background jobs such as the expiration sweep).</summary>
    Task<IReadOnlyCollection<int>> GetRestaurantIds(CancellationToken cancellationToken);
}
