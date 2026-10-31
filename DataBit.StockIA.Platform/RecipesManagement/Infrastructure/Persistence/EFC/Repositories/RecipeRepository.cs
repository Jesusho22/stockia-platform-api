using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace DataBit.StockIA.Platform.RecipesManagement.Infrastructure.Persistence.EFC.Repositories;

public class RecipeRepository(AppDbContext context) : BaseRepository<Recipe>(context), IRecipeRepository
{
    public override Task<Recipe?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Context.Set<Recipe>().FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Recipe>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await Context.Set<Recipe>().Where(recipe => recipe.RestaurantId == restaurantId).OrderByDescending(recipe => recipe.Id)
            .ToListAsync(cancellationToken);
}
