using DataBit.StockIA.Platform.SalesOrder.Domain.Repositories;
using DataBit.StockIA.Platform.SalesOrder.Interfaces.Acl;
using DataBit.StockIA.Platform.Shared.Application;

namespace DataBit.StockIA.Platform.SalesOrder.Application.Acl;

public class SalesOrderContextFacade(ISaleRepository saleRepository, IRestaurantClock clock) : ISalesOrderContextFacade
{
    public async Task<IReadOnlyList<DailyDishSales>> GetDailyUnitsByDish(int restaurantId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken)
    {
        var offset = clock.Now.Offset;
        var sales = await saleRepository.FindAllByRestaurantIdAsync(restaurantId,
            new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), offset),
            new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), offset), cancellationToken);
        return sales.Where(sale => sale.IsConfirmed)
            .SelectMany(sale => sale.LineItems.Select(line =>
                (Date: DateOnly.FromDateTime(sale.SaleDate.ToOffset(offset).DateTime), line.DishName, line.Quantity)))
            .GroupBy(row => (row.Date, row.DishName))
            .Select(group => new DailyDishSales(group.Key.Date, group.Key.DishName, group.Sum(row => row.Quantity)))
            .OrderBy(row => row.Date)
            .ToList();
    }

    public async Task<SalesSummary> GetSummary(int restaurantId, CancellationToken cancellationToken)
    {
        var sales = await saleRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken: cancellationToken);
        var confirmed = sales.Where(sale => sale.IsConfirmed).ToList();
        var today = clock.Today;
        var offset = clock.Now.Offset;
        return new SalesSummary(confirmed.Count, confirmed.Sum(sale => sale.Total),
            confirmed.Where(sale => DateOnly.FromDateTime(sale.SaleDate.ToOffset(offset).DateTime) == today).Sum(sale => sale.Total));
    }
}
