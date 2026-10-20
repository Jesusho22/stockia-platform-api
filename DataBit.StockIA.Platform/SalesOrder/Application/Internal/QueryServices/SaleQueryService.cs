using DataBit.StockIA.Platform.SalesOrder.Application.QueryServices;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Queries;
using DataBit.StockIA.Platform.SalesOrder.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Application;

namespace DataBit.StockIA.Platform.SalesOrder.Application.Internal.QueryServices;

public class SaleQueryService(ISaleRepository saleRepository, IRestaurantClock clock) : ISaleQueryService
{
    public Task<IReadOnlyList<Sale>> Handle(GetSalesByRestaurantIdQuery query, CancellationToken cancellationToken)
    {
        var offset = clock.Now.Offset;
        DateTimeOffset? from = query.From is { } fromDay ? new DateTimeOffset(fromDay.ToDateTime(TimeOnly.MinValue), offset) : null;
        DateTimeOffset? to = query.To is { } toDay ? new DateTimeOffset(toDay.ToDateTime(TimeOnly.MaxValue), offset) : null;
        return saleRepository.FindAllByRestaurantIdAsync(query.RestaurantId, from, to, cancellationToken);
    }

    public async Task<Sale?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.FindByIdAsync(query.SaleId, cancellationToken);
        return sale?.RestaurantId == query.RestaurantId ? sale : null;
    }
}
