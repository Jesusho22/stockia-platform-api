using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.StockManagement.Domain.Services;

/// <summary>Why an ingredient of a recipe cannot be supplied.</summary>
public enum ShortageReason
{
    /// <summary>The supply is not in the inventory anymore.</summary>
    NotFound,

    /// <summary>Only expired lots have enough stock: an expired product is never sold.</summary>
    Expired,

    /// <summary>The lots that are not expired do not reach the required quantity.</summary>
    Insufficient
}

public record Shortage(string ItemName, string Unit, ShortageReason Reason, Quantity Required, Quantity Available, Quantity Expired);

public record LotDeduction(InventoryItem Item, Quantity Taken, Quantity Remaining);

public record AllocationPlan(bool CanFulfill, IReadOnlyList<Shortage> Shortages, IReadOnlyList<LotDeduction> Deductions);

/// <summary>
///     Stock allocation for a sale (same rules as the Web Application):
///     1. Expired lots are never used.
///     2. With several lots of a product, the freshest usable lot goes first
///        and the next one only covers what is missing.
///     3. If any ingredient cannot be covered nothing is deducted and the sale
///        must not be registered.
///     The price of a dish is the sum of its ingredients: required quantity ×
///     unit cost of the freshest usable lot.
/// </summary>
public static class StockAllocationService
{
    public static IReadOnlyList<InventoryItem> LotsFor(StockRequirement requirement, IReadOnlyCollection<InventoryItem> items)
    {
        var linked = items.FirstOrDefault(item => item.Id == requirement.InventoryItemId);
        var key = linked?.ProductKey ?? InventoryItem.ProductKeyOf(requirement.ItemName, requirement.Unit);
        return items.Where(item => item.ProductKey == key).ToList();
    }

    public static IReadOnlyList<InventoryItem> UsableLotsFreshestFirst(IEnumerable<InventoryItem> lots, DateOnly today) =>
        lots.Where(lot => !lot.IsExpired(today) && lot.Quantity.IsPositive)
            .OrderByDescending(lot => lot.ExpirationDate)
            .ThenByDescending(lot => lot.Id)
            .ToList();

    public static decimal UnitCostFor(StockRequirement requirement, IReadOnlyCollection<InventoryItem> items, DateOnly today)
    {
        var lots = LotsFor(requirement, items);
        var lot = UsableLotsFreshestFirst(lots, today).FirstOrDefault()
                  ?? items.FirstOrDefault(item => item.Id == requirement.InventoryItemId)
                  ?? lots.FirstOrDefault();
        return lot?.UnitCost ?? 0m;
    }

    /// <summary>Price of one portion of a dish, rounded to cents.</summary>
    public static decimal PriceOf(IEnumerable<StockRequirement> requirements, IReadOnlyCollection<InventoryItem> items, DateOnly today) =>
        Math.Round(requirements.Sum(requirement => requirement.QuantityPerPortion.ToDecimal() * UnitCostFor(requirement, items, today)), 2,
            MidpointRounding.AwayFromZero);

    public static AllocationPlan Plan(IEnumerable<StockRequirement> requirements, IReadOnlyCollection<InventoryItem> items, int portions,
        DateOnly today)
    {
        var remaining = items.ToDictionary(item => item.Id, item => item.Quantity);
        var taken = new Dictionary<int, Quantity>();
        var shortages = new List<Shortage>();

        foreach (var requirement in requirements)
        {
            var required = requirement.QuantityPerPortion * portions;
            var lots = LotsFor(requirement, items);
            if (lots.Count == 0)
            {
                shortages.Add(new Shortage(requirement.ItemName, requirement.Unit, ShortageReason.NotFound, required, Quantity.Zero,
                    Quantity.Zero));
                continue;
            }

            var usable = UsableLotsFreshestFirst(lots, today);
            var available = usable.Aggregate(Quantity.Zero, (sum, lot) => sum + remaining[lot.Id]);
            var expired = lots.Where(lot => lot.IsExpired(today)).Aggregate(Quantity.Zero, (sum, lot) => sum + lot.Quantity);
            if (available < required)
            {
                shortages.Add(new Shortage(lots[0].Name, lots[0].Unit,
                    expired.IsPositive ? ShortageReason.Expired : ShortageReason.Insufficient, required, available, expired));
                continue;
            }

            var pending = required;
            foreach (var lot in usable)
            {
                if (!pending.IsPositive) break;
                var take = Quantity.Min(pending, remaining[lot.Id]);
                if (!take.IsPositive) continue;
                remaining[lot.Id] -= take;
                taken[lot.Id] = (taken.TryGetValue(lot.Id, out var previous) ? previous : Quantity.Zero) + take;
                pending -= take;
            }
        }

        List<LotDeduction> deductions = shortages.Count > 0
            ? []
            : taken.Select(entry => new LotDeduction(items.First(item => item.Id == entry.Key), entry.Value, remaining[entry.Key])).ToList();
        return new AllocationPlan(shortages.Count == 0, shortages, deductions);
    }
}
