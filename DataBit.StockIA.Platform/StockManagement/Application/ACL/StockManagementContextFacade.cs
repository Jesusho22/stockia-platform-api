using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Application.Services;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Domain.Services;
using DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;

namespace DataBit.StockIA.Platform.StockManagement.Application.ACL;

public class StockManagementContextFacade(
    IInventoryItemRepository inventoryItemRepository,
    IStockConsumptionRepository stockConsumptionRepository,
    IInventoryItemCommandService inventoryItemCommandService,
    IRestaurantClock clock) : IStockManagementContextFacade
{
    public async Task<StockAvailability> CheckAvailability(int restaurantId, IReadOnlyList<IngredientRequirement> ingredients, int portions,
        CancellationToken cancellationToken)
    {
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        var requirements = ToRequirements(ingredients);
        var plan = StockAllocationService.Plan(requirements, items, portions, clock.Today);
        return new StockAvailability(plan.CanFulfill, StockAllocationService.PriceOf(requirements, items, clock.Today),
            plan.Shortages.Select(ToShortageInfo).ToList());
    }

    public async Task<IReadOnlyList<decimal>> PricesOf(int restaurantId, IReadOnlyList<IReadOnlyList<IngredientRequirement>> dishes,
        CancellationToken cancellationToken)
    {
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        return dishes.Select(dish => StockAllocationService.PriceOf(ToRequirements(dish), items, clock.Today)).ToList();
    }

    public async Task<IReadOnlyList<ShortageInfo>> DeductStock(int restaurantId, IReadOnlyList<IngredientRequirement> ingredients,
        int portions, CancellationToken cancellationToken)
    {
        var result = await inventoryItemCommandService.Handle(new DeductStockCommand(restaurantId, ToRequirements(ingredients), portions),
            cancellationToken);
        if (result.IsSuccess) return [];
        return result.Details is IEnumerable<Shortage> shortages
            ? shortages.Select(ToShortageInfo).ToList()
            : [new ShortageInfo(string.Empty, string.Empty, "NOT_FOUND", "0", "0", "0")];
    }

    public async Task<IReadOnlyList<InventoryLotInfo>> GetInventory(int restaurantId, CancellationToken cancellationToken)
    {
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        return items.Select(ToLotInfo).ToList();
    }

    public async Task<InventoryLotInfo?> GetInventoryLot(int restaurantId, int inventoryItemId, CancellationToken cancellationToken)
    {
        var item = await inventoryItemRepository.FindByIdAsync(inventoryItemId, cancellationToken);
        return item == null || item.RestaurantId != restaurantId ? null : ToLotInfo(item);
    }

    public async Task<IReadOnlyList<StockConsumptionInfo>> GetConsumptions(int restaurantId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken)
    {
        var consumptions = await stockConsumptionRepository.FindAllByRestaurantIdAsync(restaurantId, from, to, cancellationToken);
        return consumptions.Select(consumption => new StockConsumptionInfo(consumption.ConsumedOn, consumption.ItemName, consumption.Unit,
            consumption.Quantity.ToDecimal(), consumption.Value, consumption.DaysBeforeExpiration)).ToList();
    }

    private static List<StockRequirement> ToRequirements(IEnumerable<IngredientRequirement> ingredients) =>
        ingredients.Select(line => new StockRequirement(line.InventoryItemId, line.ItemName, line.Unit, line.QuantityPerPortion)).ToList();

    private InventoryLotInfo ToLotInfo(InventoryItem item) => new(item.Id, item.Name, item.Unit, item.Quantity.ToDecimal(),
        item.MinThreshold.ToDecimal(), item.ExpirationDate, item.DaysToExpire(clock.Today), item.UnitCost, item.StockValue,
        item.StatusOn(clock.Today).ToCode(), item.ProductKey);

    private static ShortageInfo ToShortageInfo(Shortage shortage) => new(shortage.ItemName, shortage.Unit, shortage.Reason.ToCode(),
        shortage.Required.ToString(), shortage.Available.ToString(), shortage.Expired.ToString());
}
