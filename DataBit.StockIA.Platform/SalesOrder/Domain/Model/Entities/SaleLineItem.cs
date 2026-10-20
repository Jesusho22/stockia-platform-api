namespace DataBit.StockIA.Platform.SalesOrder.Domain.Model.Entities;

public class SaleLineItem
{
    private SaleLineItem()
    {
    }

    public SaleLineItem(int recipeId, string dishName, decimal unitPrice, int quantity)
    {
        if (quantity < 1) throw new ArgumentException("A line needs at least one portion.");
        RecipeId = recipeId;
        DishName = dishName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public int RecipeId { get; private set; }
    public string DishName { get; private set; } = string.Empty;

    /// <summary>Price of one portion = sum of the cost of its ingredients when it was sold.</summary>
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }
}
