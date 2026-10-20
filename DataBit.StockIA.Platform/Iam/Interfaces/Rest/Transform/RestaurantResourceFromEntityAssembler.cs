using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Interfaces.Rest.Resources;

namespace DataBit.StockIA.Platform.Iam.Interfaces.Rest.Transform;

public static class RestaurantResourceFromEntityAssembler
{
    public static RestaurantResource ToResourceFromEntity(Restaurant restaurant) => new(
        restaurant.Id, restaurant.Name, restaurant.Address, restaurant.Phone, restaurant.Latitude, restaurant.Longitude);
}
