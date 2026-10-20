using System.Reflection;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Entities;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.ProductInventory.Domain.Services;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Tests.ProductInventory;

public class StockAllocationServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);

    private static InventoryItem Lot(int id, string name, string quantity, int shelfLife, decimal cost, int? expiresInDays = null)
    {
        var item = new InventoryItem(1, name, "kg", Quantity.Parse(quantity), Quantity.Parse("1"), StorageType.Refrigerated, shelfLife, cost, Today);
        if (expiresInDays is { } days) item.OverrideExpirationDate(Today.AddDays(days));
        typeof(InventoryItem).GetProperty(nameof(InventoryItem.Id))!.SetValue(item, id);
        return item;
    }

    private static Recipe Salad(InventoryItem chicken, InventoryItem lettuce) => new(1, "Ensalada César con Pollo",
        [new RecipeIngredient(chicken.Id, chicken.Name, Quantity.Parse("0.18"), "kg"), new RecipeIngredient(lettuce.Id, lettuce.Name, Quantity.Parse("0.12"), "kg")]);

    [Fact]
    public void Uses_the_freshest_lot_and_never_an_expired_one()
    {
        var expired = Lot(1, "Pechuga de pollo", "18", 4, 14.5m, expiresInDays: -3);
        var older = Lot(2, "Pechuga de pollo", "5", 4, 14m, expiresInDays: 2);
        var fresh = Lot(3, "pechuga  de pollo", "1.12345678901234567890123456789012", 4, 15m, expiresInDays: 4);
        var lettuce = Lot(4, "Lechuga", "2", 5, 3.5m);
        var plan = StockAllocationService.Plan(Salad(expired, lettuce), [expired, older, fresh, lettuce], 1, Today);

        Assert.True(plan.CanFulfill);
        var chicken = Assert.Single(plan.Deductions, deduction => deduction.Item.Name.Contains("pollo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, chicken.Item.Id);
        Assert.Equal("0.94345678901234567890123456789012", chicken.Remaining.ToString());
    }

    [Fact]
    public void Covers_the_rest_with_the_next_lot()
    {
        var fresh = Lot(1, "Pechuga de pollo", "0.1", 4, 15m, expiresInDays: 4);
        var older = Lot(2, "Pechuga de pollo", "5", 4, 14m, expiresInDays: 1);
        var lettuce = Lot(3, "Lechuga", "2", 5, 3.5m);
        var plan = StockAllocationService.Plan(Salad(fresh, lettuce), [fresh, older, lettuce], 1, Today);

        Assert.True(plan.CanFulfill);
        Assert.Equal("0", plan.Deductions.Single(deduction => deduction.Item.Id == 1).Remaining.ToString());
        Assert.Equal("0.08", plan.Deductions.Single(deduction => deduction.Item.Id == 2).Taken.ToString());
    }

    [Fact]
    public void An_expired_product_cannot_be_sold()
    {
        var expired = Lot(1, "Pechuga de pollo", "18", 4, 14.5m, expiresInDays: -1);
        var lettuce = Lot(2, "Lechuga", "2", 5, 3.5m);
        var plan = StockAllocationService.Plan(Salad(expired, lettuce), [expired, lettuce], 1, Today);

        Assert.False(plan.CanFulfill);
        Assert.Empty(plan.Deductions);
        Assert.Equal(ShortageReason.Expired, Assert.Single(plan.Shortages).Reason);
    }

    [Fact]
    public void Insufficient_stock_blocks_the_sale()
    {
        var chicken = Lot(1, "Pechuga de pollo", "0.1", 4, 14.5m);
        var lettuce = Lot(2, "Lechuga", "2", 5, 3.5m);
        var plan = StockAllocationService.Plan(Salad(chicken, lettuce), [chicken, lettuce], 1, Today);

        Assert.False(plan.CanFulfill);
        var shortage = Assert.Single(plan.Shortages);
        Assert.Equal(ShortageReason.Insufficient, shortage.Reason);
        Assert.Equal("0.18", shortage.Required.ToString());
        Assert.Equal("0.1", shortage.Available.ToString());
    }

    [Fact]
    public void Dish_price_is_the_sum_of_its_ingredients_with_the_freshest_lot_cost()
    {
        var expired = Lot(1, "Pechuga de pollo", "18", 4, 14.5m, expiresInDays: -3);
        var fresh = Lot(2, "Pechuga de pollo", "5", 4, 15m);
        var lettuce = Lot(3, "Lechuga", "2", 5, 3.5m);
        // 0.18 × 15 + 0.12 × 3.5 = 3.12
        Assert.Equal(3.12m, StockAllocationService.RecipePrice(Salad(expired, lettuce), [expired, fresh, lettuce], Today));
    }

    [Fact]
    public void Status_precedence_is_expired_critical_low_available()
    {
        Assert.Equal(StockStatus.Expired, Lot(1, "A", "10", 1, 1m, expiresInDays: -1).StatusOn(Today));
        Assert.Equal(StockStatus.Critical, Lot(2, "B", "0", 5, 1m).StatusOn(Today));
        Assert.Equal(StockStatus.Low, Lot(3, "C", "1", 5, 1m).StatusOn(Today));
        Assert.Equal(StockStatus.Available, Lot(4, "D", "1.5", 5, 1m).StatusOn(Today));
    }
}
