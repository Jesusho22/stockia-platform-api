namespace DataBit.StockIA.Platform.SalesOrder.Interfaces.Acl;

/// <summary>Units of a dish sold on a local calendar day (confirmed sales only).</summary>
public record DailyDishSales(DateOnly Date, string DishName, int Units);

public record SalesSummary(int ConfirmedSales, decimal Revenue, decimal RevenueToday);

/// <summary>What other bounded contexts may read from Sales &amp; Order Management.</summary>
public interface ISalesOrderContextFacade
{
    /// <summary>Sales history for the demand forecast (days without sales are simply missing).</summary>
    Task<IReadOnlyList<DailyDishSales>> GetDailyUnitsByDish(int restaurantId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<SalesSummary> GetSummary(int restaurantId, CancellationToken cancellationToken);
}
