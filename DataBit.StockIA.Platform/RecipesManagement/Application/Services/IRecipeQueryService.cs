using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Services;

public interface IRecipeQueryService
{
    Task<IReadOnlyList<Recipe>> Handle(GetRecipesByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<Recipe?> Handle(GetRecipeByIdQuery query, CancellationToken cancellationToken);
}
