using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.SalesOrder.Application.CommandServices;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Commands;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Entities;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Errors;
using DataBit.StockIA.Platform.SalesOrder.Domain.Repositories;
using DataBit.StockIA.Platform.SalesOrder.Resources;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Acl;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.SalesOrder.Application.Internal.CommandServices;

/// <summary>
///     SaleRegistrationService. In one transaction: check the stock with
///     Inventory &amp; Recipe Management (expired lots excluded) → reject without
///     writing anything when a supply is missing → create the sale priced at
///     the dish cost → discount the supplies (DishSold).
/// </summary>
public class SaleCommandService(
    ISaleRepository saleRepository,
    IProductInventoryContextFacade productInventoryContextFacade,
    IUnitOfWork unitOfWork,
    IRestaurantClock clock,
    IStringLocalizer<SalesMessages> localizer) : ISaleCommandService
{
    public async Task<Result<Sale>> Handle(RegisterSaleCommand command, CancellationToken cancellationToken)
    {
        var check = await productInventoryContextFacade.CheckRecipeSale(command.RestaurantId, command.RecipeId, command.Quantity,
            cancellationToken);
        if (check == null) return Result<Sale>.Failure(SalesError.RecipeNotFound, localizer[nameof(SalesError.RecipeNotFound)]);
        if (!check.CanFulfill) return CannotSell(check.DishName, check.Shortages);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var sale = new Sale(command.RestaurantId, clock.Now.ToUniversalTime(), command.Channel,
                [new SaleLineItem(check.RecipeId, check.DishName, check.UnitPrice, command.Quantity)]);
            await saleRepository.AddAsync(sale, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            // DishSold → RecipeStockDeductionService. Re-checked inside the transaction.
            var shortages = await productInventoryContextFacade.DeductStockForRecipe(command.RestaurantId, command.RecipeId,
                command.Quantity, cancellationToken);
            if (shortages.Count > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CannotSell(check.DishName, shortages);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<Sale>.Success(sale);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Sale>.Failure(SalesError.DatabaseError, localizer[nameof(SalesError.DatabaseError)]);
        }
    }

    public async Task<Result<Sale>> Handle(VoidSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.FindByIdAsync(command.SaleId, cancellationToken);
        if (sale == null || sale.RestaurantId != command.RestaurantId)
            return Result<Sale>.Failure(SalesError.SaleNotFound, localizer[nameof(SalesError.SaleNotFound)]);
        if (!sale.Void()) return Result<Sale>.Failure(SalesError.AlreadyVoided, localizer[nameof(SalesError.AlreadyVoided)]);

        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Sale>.Success(sale);
    }

    private Result<Sale> CannotSell(string dishName, IReadOnlyList<ShortageInfo> shortages) =>
        Result<Sale>.Failure(SalesError.CannotSell, localizer[nameof(SalesError.CannotSell), dishName],
            new { dish = dishName, shortages });
}
