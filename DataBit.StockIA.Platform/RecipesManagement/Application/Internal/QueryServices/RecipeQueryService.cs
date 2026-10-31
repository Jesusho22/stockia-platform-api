using DataBit.StockIA.Platform.RecipesManagement.Application.Services;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Queries;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Internal.QueryServices;

public class RecipeQueryService(IRecipeRepository recipeRepository) : IRecipeQueryService
{
    public Task<IReadOnlyList<Recipe>> Handle(GetRecipesByRestaurantIdQuery query, CancellationToken cancellationToken) =>
        recipeRepository.FindAllByRestaurantIdAsync(query.RestaurantId, cancellationToken);

    public async Task<Recipe?> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(query.RecipeId, cancellationToken);
        return recipe?.RestaurantId == query.RestaurantId ? recipe : null;
    }
}
