using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Commands;

namespace DataBit.StockIA.Platform.StockManagement.Application.Services;

public interface IInventoryItemCommandService
{
    Task<Result<InventoryItem>> Handle(CreateInventoryItemCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(UpdateInventoryItemCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteInventoryItemCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeductStockCommand command, CancellationToken cancellationToken);
}
