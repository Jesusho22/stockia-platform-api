using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Alerts.Interfaces.Acl;
using DataBit.StockIA.Platform.DemandForecasting.Application.CommandServices;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Commands;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Services;
using DataBit.StockIA.Platform.Iam.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Entities;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.ProductInventory.Domain.Services;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Entities;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Interfaces.Acl;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Seeding;

/// <summary>
///     Creates the database and loads the data the platform needs:
///     - Always: the plan catalog (Esencial, Profesional, IoT Completo).
///     - With Seed:DemoData = true and no users yet: the demo restaurant of the
///       Web Application (same accounts as the mock API), its supplies, recipes,
///       120 days of sales history (so the ML forecast works from day one),
///       automatic alerts and a first forecast with recommendations.
///     Dates are relative to the seeding day.
/// </summary>
public class DatabaseSeeder(
    AppDbContext context,
    IHashingService hashingService,
    ISubscriptionContextFacade subscriptionContextFacade,
    IAlertsContextFacade alertsContextFacade,
    IDemandForecastCommandService demandForecastCommandService,
    IRestaurantClock clock,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    public const string DemoPassword = "stockia123";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await CreateDatabaseAsync(cancellationToken);
        await SeedPlansAsync(cancellationToken);
        if (bool.TryParse(configuration["Seed:DemoData"], out var demo) && demo && !await context.Set<User>().AnyAsync(cancellationToken))
            await SeedDemoRestaurantAsync(cancellationToken);
    }

    /// <summary>MySQL may still be starting (docker compose, CI): retry for about a minute.</summary>
    private async Task CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < 20)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, exception.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    private async Task SeedPlansAsync(CancellationToken cancellationToken)
    {
        if (await context.Set<Plan>().AnyAsync(cancellationToken)) return;
        context.Set<Plan>().AddRange(
            new Plan("Esencial", 0m, ["Hasta 50 insumos", "Registro de ventas básico", "1 usuario"], false),
            new Plan("Profesional", 39m, ["Insumos ilimitados", "Predicción de demanda con IA", "Hasta 5 usuarios", "Recomendaciones automáticas"], true),
            new Plan("IoT Completo", 79m, ["Todo lo de Profesional", "Sensores IoT", "Alertas en tiempo real", "Usuarios ilimitados"], false));
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoRestaurantAsync(CancellationToken cancellationToken)
    {
        var today = clock.Today;
        logger.LogInformation("Seeding the demo restaurant ({Today})", today);

        var restaurant = new Restaurant("DataBite Corp — Restaurante Demo").UpdateProfile("DataBite Corp — Restaurante Demo",
            "Av. José Larco 123, Miraflores, Lima", "+51 987 654 321");
        context.Set<Restaurant>().Add(restaurant);
        await context.SaveChangesAsync(cancellationToken);

        var admin = new User("Carla Gallardo", "admin@databitecorp.com", hashingService.HashPassword(DemoPassword), restaurant.Id, UserRole.Admin);
        var employee = new User("Jesus Miranda", "empleado@databitecorp.com", hashingService.HashPassword(DemoPassword), restaurant.Id,
            UserRole.Employee);
        context.Set<User>().AddRange(admin, employee);
        await context.SaveChangesAsync(cancellationToken);
        restaurant.LinkOwner(admin.Id);
        await context.SaveChangesAsync(cancellationToken);
        await subscriptionContextFacade.CreateDefaultSubscription(restaurant.Id, cancellationToken);

        InventoryItem Item(string name, string quantity, string min, StorageType storage, int shelfLife, decimal cost) =>
            new(restaurant.Id, name, "kg", Quantity.Parse(quantity), Quantity.Parse(min), storage, shelfLife, cost, today);

        // An expired chicken lot and a fresh one: sales must use the fresh lot.
        var expiredChicken = Item("Pechuga de pollo", "18", "10", StorageType.Refrigerated, 4, 14.5m).OverrideExpirationDate(today.AddDays(-3));
        var flour = Item("Harina de trigo", "32", "15", StorageType.Ambient, 180, 3.2m);
        var cheese = Item("Queso mozzarella", "4", "6", StorageType.Refrigerated, 10, 22m);
        var tomato = Item("Tomate", "9", "8", StorageType.Ambient, 6, 4.1m).OverrideExpirationDate(today.AddDays(1));
        var beef = Item("Lomo fino", "0", "5", StorageType.Frozen, 30, 32m);
        var lettuce = Item("Lechuga", "6", "4", StorageType.Refrigerated, 5, 3.5m);
        var potato = Item("Papa", "40", "20", StorageType.Ambient, 45, 2.1m);
        var freshChicken = Item("Pechuga de pollo", "12.5", "10", StorageType.Refrigerated, 4, 15m);
        // A product whose only lot is expired: its dish cannot be sold.
        var cream = new InventoryItem(restaurant.Id, "Crema de leche", "L", Quantity.Parse("3"), Quantity.Parse("1"), StorageType.Refrigerated,
            7, 12m, today).OverrideExpirationDate(today.AddDays(-2));
        var pasta = Item("Fettuccine", "5", "2", StorageType.Ambient, 120, 8m);
        var items = new[] { expiredChicken, flour, cheese, tomato, beef, lettuce, potato, freshChicken, cream, pasta };
        context.Set<InventoryItem>().AddRange(items);
        await context.SaveChangesAsync(cancellationToken);

        RecipeIngredient Line(InventoryItem item, string quantity) => new(item.Id, item.Name, Quantity.Parse(quantity), item.Unit);
        var recipes = new[]
        {
            new Recipe(restaurant.Id, "Pizza Margarita", [Line(flour, "0.3"), Line(cheese, "0.2"), Line(tomato, "0.15")]),
            new Recipe(restaurant.Id, "Lomo Saltado", [Line(beef, "0.25"), Line(potato, "0.3"), Line(tomato, "0.1")]),
            new Recipe(restaurant.Id, "Ensalada César con Pollo", [Line(freshChicken, "0.18"), Line(lettuce, "0.12")]),
            new Recipe(restaurant.Id, "Fettuccine Alfredo", [Line(pasta, "0.15"), Line(cream, "0.1")])
        };
        context.Set<Recipe>().AddRange(recipes);
        await context.SaveChangesAsync(cancellationToken);

        SeedSalesHistory(restaurant.Id, recipes, items, today);
        await context.SaveChangesAsync(cancellationToken);

        await alertsContextFacade.EvaluateInventoryAlerts(restaurant.Id, cancellationToken);
        try
        {
            await demandForecastCommandService.Handle(new GenerateDemandForecastCommand(restaurant.Id), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The first demand forecast could not be generated");
        }
    }

    /// <summary>
    ///     120 days of daily POS sales with the same patterns the model was
    ///     trained on (weekday, holidays, quincena) plus noise. Historic sales do
    ///     not discount stock.
    /// </summary>
    private void SeedSalesHistory(int restaurantId, IReadOnlyList<Recipe> recipes, IReadOnlyCollection<InventoryItem> items, DateOnly today)
    {
        var random = new Random(2026);
        double[] weekdayFactors = [0.80, 0.85, 0.90, 0.95, 1.15, 1.35, 1.20];
        // Fettuccine Alfredo is new on the menu: it has no sales history yet.
        var baseUnits = new Dictionary<string, double> { ["Pizza Margarita"] = 28, ["Lomo Saltado"] = 32, ["Ensalada César con Pollo"] = 22 };
        var offset = clock.Now.Offset;

        for (var daysBack = 120; daysBack >= 1; daysBack--)
        {
            var day = today.AddDays(-daysBack);
            var factor = weekdayFactors[PeruCalendar.MondayBasedDayOfWeek(day)];
            if (PeruCalendar.IsHoliday(day)) factor *= 1.2;
            else if (PeruCalendar.IsHolidayEve(day)) factor *= 1.07;
            if (PeruCalendar.IsPaydayWindow(day)) factor *= 1.12;

            var lines = recipes.Where(recipe => baseUnits.ContainsKey(recipe.DishName)).Select(recipe =>
            {
                var noise = Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
                var units = (int)Math.Round(baseUnits[recipe.DishName] * factor * (1 + 0.18 * noise));
                return (recipe, units: Math.Max(0, units));
            }).Where(line => line.units > 0).ToList();
            if (lines.Count == 0) continue;

            var saleDate = new DateTimeOffset(day.ToDateTime(new TimeOnly(21, 0)), offset).ToUniversalTime();
            context.Set<Sale>().Add(new Sale(restaurantId, saleDate, SaleChannel.Pos, lines.Select(line =>
                new SaleLineItem(line.recipe.Id, line.recipe.DishName, StockAllocationService.RecipePrice(line.recipe, items, today), line.units))));
        }
    }
}
