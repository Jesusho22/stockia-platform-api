using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.RecipesManagement.Application.Errors;
using DataBit.StockIA.Platform.RecipesManagement.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.RecipesManagement.Application.Services;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Entities;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;
using DataBit.StockIA.Platform.RecipesManagement.Resources;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Internal.CommandServices;

/// <summary>
///     Ingredient Deduction Engine (C4). When a dish is sold ("Dish Sold"), in
///     one transaction: check the stock with Stock Management (expired lots
///     excluded) → reject without writing anything when a supply is missing →
///     record the sale priced at the dish cost → take out the supplies.
/// </summary>
public class SaleCommandService(
    ISaleRepository saleRepository,
    IRecipeRepository recipeRepository,
    ExternalStockService externalStockService,
    IUnitOfWork unitOfWork,
    IRestaurantClock clock,
    IStringLocalizer<RecipesManagementMessages> localizer) : ISaleCommandService
{
    public async Task<Result<Sale>> Handle(RegisterSaleCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(command.RecipeId, cancellationToken);
        if (recipe == null || recipe.RestaurantId != command.RestaurantId)
            return Result<Sale>.Failure(RecipesManagementError.RecipeNotFound, localizer["RecipeToSellNotFound"]);

        var availability = await externalStockService.CheckAvailability(recipe, command.Quantity, cancellationToken);
        if (!availability.CanFulfill) return CannotSell(recipe.DishName, availability.Shortages);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var sale = new Sale(command.RestaurantId, clock.Now.ToUniversalTime(), command.Channel,
                [new SaleLineItem(recipe.Id, recipe.DishName, availability.UnitPrice, command.Quantity)]);
            await saleRepository.AddAsync(sale, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            // Dish Sold → take out the supplies. Re-checked inside the transaction.
            var shortages = await externalStockService.DeductStock(recipe, command.Quantity, cancellationToken);
            if (shortages.Count > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CannotSell(recipe.DishName, shortages);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<Sale>.Success(sale);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Sale>.Failure(RecipesManagementError.DatabaseError, localizer[nameof(RecipesManagementError.DatabaseError)]);
        }
    }

    public async Task<Result<Sale>> Handle(VoidSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.FindByIdAsync(command.SaleId, cancellationToken);
        if (sale == null || sale.RestaurantId != command.RestaurantId)
            return Result<Sale>.Failure(RecipesManagementError.SaleNotFound, localizer[nameof(RecipesManagementError.SaleNotFound)]);
        if (!sale.Void())
            return Result<Sale>.Failure(RecipesManagementError.AlreadyVoided, localizer[nameof(RecipesManagementError.AlreadyVoided)]);

        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Sale>.Success(sale);
    }

    private Result<Sale> CannotSell(string dishName, IReadOnlyList<ShortageInfo> shortages) =>
        Result<Sale>.Failure(RecipesManagementError.CannotSell, localizer[nameof(RecipesManagementError.CannotSell), dishName],
            new { dish = dishName, shortages });
}
