using DataBit.StockIA.Platform.RecipesManagement.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.ACL;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.ACL;

public class RecipesManagementContextFacade(
    IRecipeRepository recipeRepository,
    ISaleRepository saleRepository,
    ExternalStockService externalStockService,
    IRestaurantClock clock) : IRecipesManagementContextFacade
{
    public async Task<IReadOnlyList<RecipeInfo>> GetActiveRecipes(int restaurantId, CancellationToken cancellationToken)
    {
        var recipes = (await recipeRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken))
            .Where(recipe => recipe.Active).ToList();
        if (recipes.Count == 0) return [];
        var prices = await externalStockService.PricesOf(restaurantId, recipes, cancellationToken);
        var lots = await externalStockService.GetInventory(restaurantId, cancellationToken);
        return recipes.Select((recipe, index) => new RecipeInfo(recipe.Id, recipe.DishName, prices[index],
            recipe.Ingredients.Select(line => new RecipeIngredientInfo(line.InventoryItemId, line.InventoryItemName, line.Unit,
                line.QuantityRequired.ToDecimal(),
                lots.FirstOrDefault(lot => lot.InventoryItemId == line.InventoryItemId)?.ProductKey
                ?? ProductKeyOf(line.InventoryItemName, line.Unit))).ToList())).ToList();
    }

    public async Task<IReadOnlyList<DailyDishSales>> GetDailyUnitsByDish(int restaurantId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken)
    {
        var offset = clock.Now.Offset;
        return (await ConfirmedSales(restaurantId, from, to, cancellationToken))
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

    public async Task<SalesPeriodSummary> GetSalesBetween(int restaurantId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var sales = await ConfirmedSales(restaurantId, from, to, cancellationToken);
        return new SalesPeriodSummary(from, to, sales.Count, sales.Sum(sale => sale.LineItems.Sum(line => line.Quantity)),
            sales.Sum(sale => sale.Total));
    }

    private async Task<List<Sale>> ConfirmedSales(int restaurantId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var offset = clock.Now.Offset;
        var sales = await saleRepository.FindAllByRestaurantIdAsync(restaurantId,
            new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), offset),
            new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), offset), cancellationToken);
        return sales.Where(sale => sale.IsConfirmed).ToList();
    }

    /// <summary>Same normalization as Stock Management, for an ingredient whose lot was deleted.</summary>
    private static string ProductKeyOf(string name, string unit)
    {
        static string Normalize(string text) => string.Join(' ', text.Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return $"{Normalize(name)}|{Normalize(unit)}";
    }
}
