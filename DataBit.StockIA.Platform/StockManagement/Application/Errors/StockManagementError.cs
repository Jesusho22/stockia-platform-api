namespace DataBit.StockIA.Platform.StockManagement.Application.Errors;

/// <summary>Errors of the Stock Management bounded context (their names are the problem titles).</summary>
public enum StockManagementError
{
    InventoryItemNotFound,
    InvalidData,
    InsufficientStock
}
