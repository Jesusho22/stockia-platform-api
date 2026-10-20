using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;

public interface IRecipeCommandService
{
    Task<Result<Recipe>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken);
    Task<Result<Recipe>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken);
}
