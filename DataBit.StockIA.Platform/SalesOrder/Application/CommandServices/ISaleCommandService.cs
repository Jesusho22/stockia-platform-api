using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.SalesOrder.Application.CommandServices;

public interface ISaleCommandService
{
    Task<Result<Sale>> Handle(RegisterSaleCommand command, CancellationToken cancellationToken);
    Task<Result<Sale>> Handle(VoidSaleCommand command, CancellationToken cancellationToken);
}
