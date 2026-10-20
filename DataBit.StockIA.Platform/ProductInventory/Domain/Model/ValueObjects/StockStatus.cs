namespace DataBit.StockIA.Platform.ProductInventory.Domain.Model.ValueObjects;

/// <summary>Precedence: Expired &gt; Critical (no stock) &gt; Low (at or below the minimum) &gt; Available.</summary>
public enum StockStatus
{
    Available,
    Low,
    Critical,
    Expired
}
