using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Entities;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;

/// <summary>
///     A sale of the restaurant. It is only created after the stock was
///     validated, so the sales history never contains a sale without stock or
///     with expired supplies.
/// </summary>
public class Sale : IAuditableEntity
{
    private readonly List<SaleLineItem> _lineItems = [];

    private Sale()
    {
    }

    public Sale(int restaurantId, DateTimeOffset saleDate, SaleChannel channel, IEnumerable<SaleLineItem> lineItems)
    {
        RestaurantId = restaurantId;
        SaleDate = saleDate;
        Channel = channel;
        _lineItems.AddRange(lineItems);
        if (_lineItems.Count == 0) throw new ArgumentException("A sale needs at least one line.");
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public DateTimeOffset SaleDate { get; private set; }
    public SaleChannel Channel { get; private set; }
    public SaleStatus Status { get; private set; } = SaleStatus.Confirmed;
    public IReadOnlyList<SaleLineItem> LineItems => _lineItems;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public decimal Total => _lineItems.Sum(line => line.UnitPrice * line.Quantity);
    public bool IsConfirmed => Status == SaleStatus.Confirmed;

    /// <summary>Voids the sale. The supplies already used are not returned to stock.</summary>
    public bool Void()
    {
        if (!IsConfirmed) return false;
        Status = SaleStatus.Voided;
        return true;
    }
}
