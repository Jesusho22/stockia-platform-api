using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;

public interface IRecipeRepository : IBaseRepository<Recipe>
{
    Task<IReadOnlyList<Recipe>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
