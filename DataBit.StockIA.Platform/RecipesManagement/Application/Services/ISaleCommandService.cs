using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Patterns;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Services;

public interface ISaleCommandService
{
    Task<Result<Sale>> Handle(RegisterSaleCommand command, CancellationToken cancellationToken);
    Task<Result<Sale>> Handle(VoidSaleCommand command, CancellationToken cancellationToken);
}
