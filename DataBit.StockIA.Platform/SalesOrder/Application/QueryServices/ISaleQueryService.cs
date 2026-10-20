using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Queries;

namespace DataBit.StockIA.Platform.SalesOrder.Application.QueryServices;

public interface ISaleQueryService
{
    Task<IReadOnlyList<Sale>> Handle(GetSalesByRestaurantIdQuery query, CancellationToken cancellationToken);
    Task<Sale?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken);
}
