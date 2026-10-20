using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Entities;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.ProductInventory.Domain.Services;

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
///     Stock allocation for a sale (same rules as the Web Application's
///     stock-allocation.js):
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
    public static IReadOnlyList<InventoryItem> LotsForLine(RecipeIngredient line, IReadOnlyCollection<InventoryItem> items)
    {
        var linked = items.FirstOrDefault(item => item.Id == line.InventoryItemId);
        var key = linked?.ProductKey ?? InventoryItem.ProductKeyOf(line.InventoryItemName, line.Unit);
        return items.Where(item => item.ProductKey == key).ToList();
    }

    public static IReadOnlyList<InventoryItem> UsableLotsFreshestFirst(IEnumerable<InventoryItem> lots, DateOnly today) =>
        lots.Where(lot => !lot.IsExpired(today) && lot.Quantity.IsPositive)
            .OrderByDescending(lot => lot.ExpirationDate)
            .ThenByDescending(lot => lot.Id)
            .ToList();

    public static decimal UnitCostForLine(RecipeIngredient line, IReadOnlyCollection<InventoryItem> items, DateOnly today)
    {
        var lots = LotsForLine(line, items);
        var lot = UsableLotsFreshestFirst(lots, today).FirstOrDefault()
                  ?? items.FirstOrDefault(item => item.Id == line.InventoryItemId)
                  ?? lots.FirstOrDefault();
        return lot?.UnitCost ?? 0m;
    }

    /// <summary>Price of one portion of the dish, rounded to cents.</summary>
    public static decimal RecipePrice(Recipe recipe, IReadOnlyCollection<InventoryItem> items, DateOnly today) =>
        Math.Round(recipe.Ingredients.Sum(line => line.QuantityRequired.ToDecimal() * UnitCostForLine(line, items, today)), 2,
            MidpointRounding.AwayFromZero);

    public static AllocationPlan Plan(Recipe recipe, IReadOnlyCollection<InventoryItem> items, int portions, DateOnly today)
    {
        var remaining = items.ToDictionary(item => item.Id, item => item.Quantity);
        var taken = new Dictionary<int, Quantity>();
        var shortages = new List<Shortage>();

        foreach (var line in recipe.Ingredients)
        {
            var required = line.QuantityRequired * portions;
            var lots = LotsForLine(line, items);
            if (lots.Count == 0)
            {
                shortages.Add(new Shortage(line.InventoryItemName, line.Unit, ShortageReason.NotFound, required, Quantity.Zero, Quantity.Zero));
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
