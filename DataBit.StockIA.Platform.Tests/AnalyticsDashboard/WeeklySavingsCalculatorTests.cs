using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Services;

namespace DataBit.StockIA.Platform.Tests.AnalyticsDashboard;

public class WeeklySavingsCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 30);
    private static readonly SalesTotals NoSales = new(0, 0, 0m);

    [Fact]
    public void The_week_is_the_last_seven_days_including_today()
    {
        var (from, to) = WeeklySavingsCalculator.WeekEndingOn(Today);
        Assert.Equal(new DateOnly(2026, 10, 24), from);
        Assert.Equal(Today, to);
    }

    [Fact]
    public void Stock_used_in_its_last_days_counts_as_rescued()
    {
        ConsumptionEntry[] consumptions =
        [
            new(Today.AddDays(-1), "Tomate", "kg", 2m, 8.2m, 2), // rescued: 2 days left
            new(Today.AddDays(-2), "Papa", "kg", 3m, 6.3m, 40), // plenty of time left: not rescued
            new(Today.AddDays(-9), "Tomate", "kg", 1m, 4.1m, 0) // previous week
        ];

        var savings = WeeklySavingsCalculator.Calculate(Today, consumptions, [], NoSales, NoSales, 0, null);

        Assert.Equal(8.2m, savings.RescuedValue);
        Assert.Equal(4.1m, savings.PreviousRescuedValue);
        Assert.Equal(8.2m, savings.EstimatedSavings);
        Assert.Equal("Tomate", Assert.Single(savings.TopRescued).ItemName);
    }

    [Fact]
    public void Less_waste_than_last_week_adds_to_the_savings()
    {
        LotSnapshot[] lots =
        [
            new("Crema de leche", "L", 3m, Today.AddDays(-2), 36m), // lost this week
            new("Pechuga de pollo", "kg", 18m, Today.AddDays(-10), 261m), // lost the previous week
            new("Fettuccine", "kg", 0m, Today.AddDays(-3), 0m) // empty lot: nothing lost
        ];

        var savings = WeeklySavingsCalculator.Calculate(Today, [], lots, NoSales, NoSales, 120, 0.71m);

        Assert.Equal(36m, savings.WasteValue);
        Assert.Equal(261m, savings.PreviousWasteValue);
        Assert.Equal(225m, savings.WasteReduction);
        Assert.Equal(225m, savings.EstimatedSavings);
        Assert.Equal("Crema de leche", Assert.Single(savings.Wasted).ItemName);
        Assert.Equal(120, savings.ProjectedUnitsNextWeek);
    }

    [Fact]
    public void More_waste_never_makes_the_savings_negative()
    {
        LotSnapshot[] lots = [new("Crema de leche", "L", 3m, Today.AddDays(-2), 36m)];

        var savings = WeeklySavingsCalculator.Calculate(Today, [], lots, NoSales, NoSales, 0, null);

        Assert.Equal(-36m, savings.WasteReduction);
        Assert.Equal(0m, savings.EstimatedSavings);
    }
}
