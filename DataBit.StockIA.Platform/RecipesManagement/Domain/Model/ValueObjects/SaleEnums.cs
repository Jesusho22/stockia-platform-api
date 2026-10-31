namespace DataBit.StockIA.Platform.RecipesManagement.Domain.Model.ValueObjects;

/// <summary>Where the sale was originated.</summary>
public enum SaleChannel
{
    Pos,
    Manual
}

/// <summary>A confirmed sale is immutable; it can only be voided.</summary>
public enum SaleStatus
{
    Confirmed,
    Voided
}
