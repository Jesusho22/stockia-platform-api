using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Patterns;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Services;

public interface IRecipeCommandService
{
    Task<Result<Recipe>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken);
    Task<Result<Recipe>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken);
}
