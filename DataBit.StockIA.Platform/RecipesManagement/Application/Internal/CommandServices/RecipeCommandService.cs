using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.RecipesManagement.Application.Errors;
using DataBit.StockIA.Platform.RecipesManagement.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.RecipesManagement.Application.Services;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Entities;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Repositories;
using DataBit.StockIA.Platform.RecipesManagement.Resources;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.RecipesManagement.Application.Internal.CommandServices;

/// <summary>Recipe CRUD (C4: Recipe CRUD Controller). Each ingredient must be a supply of the inventory.</summary>
public class RecipeCommandService(
    IRecipeRepository recipeRepository,
    ExternalStockService externalStockService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<RecipesManagementMessages> localizer) : IRecipeCommandService
{
    public async Task<Result<Recipe>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken)
    {
        var lines = await BuildIngredients(command.RestaurantId, command.Ingredients, cancellationToken);
        if (lines.IsFailure) return Result<Recipe>.Failure(lines.Error!, lines.Message);
        try
        {
            var recipe = new Recipe(command.RestaurantId, command.DishName, lines.Value!);
            await recipeRepository.AddAsync(recipe, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Recipe>.Success(recipe);
        }
        catch (ArgumentException)
        {
            return Result<Recipe>.Failure(RecipesManagementError.InvalidData, localizer["RecipeIncomplete"]);
        }
    }

    public async Task<Result<Recipe>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(command.RecipeId, cancellationToken);
        if (recipe == null || recipe.RestaurantId != command.RestaurantId)
            return Result<Recipe>.Failure(RecipesManagementError.RecipeNotFound, localizer[nameof(RecipesManagementError.RecipeNotFound)]);

        var lines = await BuildIngredients(command.RestaurantId, command.Ingredients, cancellationToken);
        if (lines.IsFailure) return Result<Recipe>.Failure(lines.Error!, lines.Message);
        try
        {
            recipe.Update(command.DishName, lines.Value!);
        }
        catch (ArgumentException)
        {
            return Result<Recipe>.Failure(RecipesManagementError.InvalidData, localizer["RecipeIncomplete"]);
        }

        // Tracked aggregate: EF detects the replaced ingredient lines by itself.
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Recipe>.Success(recipe);
    }

    public async Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(command.RecipeId, cancellationToken);
        if (recipe == null || recipe.RestaurantId != command.RestaurantId)
            return Result.Failure(RecipesManagementError.RecipeNotFound, localizer[nameof(RecipesManagementError.RecipeNotFound)]);

        recipeRepository.Remove(recipe);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Each line takes the name and unit of its supply; repeated supplies keep the last quantity.</summary>
    private async Task<Result<List<RecipeIngredient>>> BuildIngredients(int restaurantId, IReadOnlyList<RecipeIngredientData> data,
        CancellationToken cancellationToken)
    {
        var lots = await externalStockService.GetInventory(restaurantId, cancellationToken);
        var lines = new Dictionary<int, RecipeIngredient>();
        foreach (var line in data)
        {
            var lot = lots.FirstOrDefault(candidate => candidate.InventoryItemId == line.InventoryItemId);
            if (lot == null)
                return Result<List<RecipeIngredient>>.Failure(RecipesManagementError.IngredientNotFound,
                    localizer[nameof(RecipesManagementError.IngredientNotFound), line.InventoryItemId]);
            if (!line.QuantityRequired.IsPositive)
                return Result<List<RecipeIngredient>>.Failure(RecipesManagementError.InvalidData, localizer["IngredientQuantity"]);
            lines[lot.InventoryItemId] = new RecipeIngredient(lot.InventoryItemId, lot.Name, line.QuantityRequired, lot.Unit);
        }

        return Result<List<RecipeIngredient>>.Success(lines.Values.ToList());
    }
}
