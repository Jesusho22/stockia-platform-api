using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;

public interface IRecipeRepository : IBaseRepository<Recipe>
{
    Task<IReadOnlyList<Recipe>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
}
