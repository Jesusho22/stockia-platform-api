using System.Globalization;
using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Alerts.Domain.Services;

public record IngredientNeed(string ProductKey, string ItemName, string Unit, decimal QuantityPerPortion);

public record DishPlan(string DishName, int ProjectedUnits, int UnitsSoldLastWeek, IReadOnlyList<IngredientNeed> Ingredients);

public record ProductStock(string ProductKey, string Name, string Unit, decimal UsableQuantity);

public record RecommendationProposal(RecommendationType Type, string Message, string ExpectedImpact);

/// <summary>
///     Turns a demand forecast into recommendations:
///     - Purchase: projected need of a supply (Σ projected portions × quantity
///       per portion) greater than its usable stock (lots not expired).
///     - Menu: dishes whose projection grows ≥ 15% (highlight them) or falls
///       ≥ 20% (promote them to avoid waste) against last week's sales.
/// </summary>
public static class RecommendationPolicy
{
    public const decimal GrowthThreshold = 0.15m;
    public const decimal DeclineThreshold = 0.20m;

    public static IReadOnlyList<RecommendationProposal> Propose(IReadOnlyList<DishPlan> dishes, IReadOnlyList<ProductStock> stock,
        DateOnly horizonEnd)
    {
        var proposals = new List<RecommendationProposal>();
        var needs = dishes
            .SelectMany(dish => dish.Ingredients.Select(line => (Line: line, Dish: dish.DishName, Need: line.QuantityPerPortion * dish.ProjectedUnits)))
            .GroupBy(entry => entry.Line.ProductKey);

        foreach (var group in needs)
        {
            var first = group.First().Line;
            var need = group.Sum(entry => entry.Need);
            var usable = stock.FirstOrDefault(product => product.ProductKey == group.Key)?.UsableQuantity ?? 0m;
            if (need <= usable) continue;
            var toBuy = Math.Ceiling((need - usable) * 100) / 100;
            var mainDish = group.OrderByDescending(entry => entry.Need).First().Dish;
            proposals.Add(new RecommendationProposal(RecommendationType.PurchaseSuggestion,
                $"Comprar {Format(toBuy)} {first.Unit} de {first.ItemName} antes del {horizonEnd:dd/MM}: la demanda proyectada necesita " +
                $"{Format(need)} {first.Unit} y hay {Format(usable)} {first.Unit} utilizables.",
                $"Evita quiebre de stock en {mainDish}"));
        }

        foreach (var dish in dishes.Where(dish => dish.UnitsSoldLastWeek > 0))
        {
            var change = (dish.ProjectedUnits - dish.UnitsSoldLastWeek) / (decimal)dish.UnitsSoldLastWeek;
            if (change >= GrowthThreshold)
                proposals.Add(new RecommendationProposal(RecommendationType.MenuAdjustment,
                    $"La demanda de {dish.DishName} subiría {Percent(change)} la próxima semana; considera destacarla en el menú.",
                    $"+{Percent(change)} de ventas esperadas"));
            else if (change <= -DeclineThreshold)
                proposals.Add(new RecommendationProposal(RecommendationType.MenuAdjustment,
                    $"La demanda de {dish.DishName} bajaría {Percent(-change)}; promociónala o reduce la compra de sus insumos.",
                    "Reduce el desperdicio de insumos"));
        }

        return proposals;
    }

    private static string Format(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Percent(decimal ratio) => (ratio * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
}
