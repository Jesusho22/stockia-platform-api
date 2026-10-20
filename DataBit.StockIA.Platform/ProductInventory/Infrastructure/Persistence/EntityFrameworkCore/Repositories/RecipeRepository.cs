using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.ProductInventory.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class RecipeRepository(AppDbContext context) : BaseRepository<Recipe>(context), IRecipeRepository
{
    public override Task<Recipe?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Context.Set<Recipe>().FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Recipe>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await Context.Set<Recipe>().Where(recipe => recipe.RestaurantId == restaurantId).OrderByDescending(recipe => recipe.Id)
            .ToListAsync(cancellationToken);
}
