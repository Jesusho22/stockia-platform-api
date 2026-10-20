using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;

public interface IInventoryItemCommandService
{
    Task<Result<InventoryItem>> Handle(CreateInventoryItemCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(UpdateInventoryItemCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteInventoryItemCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeductStockForRecipeCommand command, CancellationToken cancellationToken);
}
