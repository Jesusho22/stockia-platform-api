using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Services;

/// <summary>
///     Weekly Savings Calculator (C4). The week is the last 7 days including
///     today, compared with the 7 days before.
///     <list type="bullet">
///         <item>Rescued value: stock used by sales when its lot had at most <see cref="RescueWindowDays" /> days left.</item>
///         <item>Waste value: lots that expired during the week while still holding stock.</item>
///         <item>Estimated savings = rescued value + reduction of the waste against the previous week (only when it went down).</item>
///     </list>
/// </summary>
public static class WeeklySavingsCalculator
{
    /// <summary>Same window as the "expiring soon" status of Stock Management.</summary>
    public const int RescueWindowDays = 3;

    public const int WeekDays = 7;

    public static (DateOnly From, DateOnly To) WeekEndingOn(DateOnly today) => (today.AddDays(-(WeekDays - 1)), today);

    public static WeeklySavings Calculate(DateOnly today, IReadOnlyCollection<ConsumptionEntry> consumptions,
        IReadOnlyCollection<LotSnapshot> lots, SalesTotals currentSales, SalesTotals previousSales, int projectedUnitsNextWeek,
        decimal? forecastConfidence)
    {
        var (from, to) = WeekEndingOn(today);
        var previousFrom = from.AddDays(-WeekDays);
        var previousTo = from.AddDays(-1);

        var rescued = Rescued(consumptions, from, to).ToList();
        var previousRescued = Rescued(consumptions, previousFrom, previousTo).ToList();
        var wasted = ExpiredBetween(lots, from, to).ToList();
        var previousWasted = ExpiredBetween(lots, previousFrom, previousTo).ToList();

        var rescuedValue = rescued.Sum(entry => entry.Value);
        var wasteValue = wasted.Sum(lot => lot.StockValue);
        var previousWasteValue = previousWasted.Sum(lot => lot.StockValue);
        var wasteReduction = previousWasteValue - wasteValue;

        return new WeeklySavings(from, to,
            Round(rescuedValue + Math.Max(0m, wasteReduction)),
            Round(rescuedValue),
            Round(previousRescued.Sum(entry => entry.Value)),
            Round(wasteValue),
            Round(previousWasteValue),
            Round(wasteReduction),
            currentSales.ConfirmedSales,
            currentSales.PortionsSold,
            Round(currentSales.Revenue),
            Round(previousSales.Revenue),
            projectedUnitsNextWeek,
            forecastConfidence,
            rescued.GroupBy(entry => (entry.ItemName, entry.Unit))
                .Select(group => new SavingsLine(group.Key.ItemName, group.Key.Unit, group.Sum(entry => entry.Quantity),
                    Round(group.Sum(entry => entry.Value))))
                .OrderByDescending(line => line.Value).Take(5).ToList(),
            wasted.GroupBy(lot => (lot.Name, lot.Unit))
                .Select(group => new SavingsLine(group.Key.Name, group.Key.Unit, group.Sum(lot => lot.Quantity),
                    Round(group.Sum(lot => lot.StockValue))))
                .OrderByDescending(line => line.Value).ToList());
    }

    private static IEnumerable<ConsumptionEntry> Rescued(IEnumerable<ConsumptionEntry> consumptions, DateOnly from, DateOnly to) =>
        consumptions.Where(entry => entry.ConsumedOn >= from && entry.ConsumedOn <= to &&
                                    entry.DaysBeforeExpiration is >= 0 and <= RescueWindowDays);

    /// <summary>A lot is lost the day after its expiration date (its last usable day).</summary>
    private static IEnumerable<LotSnapshot> ExpiredBetween(IEnumerable<LotSnapshot> lots, DateOnly from, DateOnly to) =>
        lots.Where(lot => lot.Quantity > 0 && lot.ExpirationDate.AddDays(1) >= from && lot.ExpirationDate.AddDays(1) <= to);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
