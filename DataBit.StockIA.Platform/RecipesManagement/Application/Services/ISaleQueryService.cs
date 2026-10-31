using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Services;

public interface ISaleQueryService
{
    Task<IReadOnlyList<Sale>> Handle(GetSalesByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<Sale?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken);
}
