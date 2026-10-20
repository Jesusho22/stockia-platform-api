namespace DataBit.StockIA.Platform.Iam.Interfaces.Acl;

/// <summary>
///     What other bounded contexts may read from IAM.
/// </summary>
public interface IIamContextFacade
{
    /// <summary>Coordinates of the restaurant for the weather of the demand forecast.</summary>
    Task<(double latitude, double longitude)?> GetRestaurantLocation(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Every restaurant id (background jobs such as the expiration sweep).</summary>
    Task<IReadOnlyCollection<int>> GetRestaurantIds(CancellationToken cancellationToken);
}
