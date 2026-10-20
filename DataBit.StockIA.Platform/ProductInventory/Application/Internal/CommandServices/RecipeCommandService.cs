using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Entities;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Errors;
using DataBit.StockIA.Platform.ProductInventory.Domain.Repositories;
using DataBit.StockIA.Platform.ProductInventory.Resources;
using DataBit.StockIA.Platform.Shared.Application.Model;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.ProductInventory.Application.Internal.CommandServices;

public class RecipeCommandService(
    IRecipeRepository recipeRepository,
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ProductInventoryMessages> localizer) : IRecipeCommandService
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
            return Result<Recipe>.Failure(ProductInventoryError.InvalidData, localizer["RecipeIncomplete"]);
        }
    }

    public async Task<Result<Recipe>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(command.RecipeId, cancellationToken);
        if (recipe == null || recipe.RestaurantId != command.RestaurantId)
            return Result<Recipe>.Failure(ProductInventoryError.RecipeNotFound, localizer[nameof(ProductInventoryError.RecipeNotFound)]);

        var lines = await BuildIngredients(command.RestaurantId, command.Ingredients, cancellationToken);
        if (lines.IsFailure) return Result<Recipe>.Failure(lines.Error!, lines.Message);
        try
        {
            recipe.Update(command.DishName, lines.Value!);
        }
        catch (ArgumentException)
        {
            return Result<Recipe>.Failure(ProductInventoryError.InvalidData, localizer["RecipeIncomplete"]);
        }

        // Tracked aggregate: EF detects the replaced ingredient lines by itself.
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Recipe>.Success(recipe);
    }

    public async Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.FindByIdAsync(command.RecipeId, cancellationToken);
        if (recipe == null || recipe.RestaurantId != command.RestaurantId)
            return Result.Failure(ProductInventoryError.RecipeNotFound, localizer[nameof(ProductInventoryError.RecipeNotFound)]);

        recipeRepository.Remove(recipe);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Each line takes the name and unit of its supply; repeated supplies keep the last quantity.</summary>
    private async Task<Result<List<RecipeIngredient>>> BuildIngredients(int restaurantId, IReadOnlyList<RecipeIngredientData> data,
        CancellationToken cancellationToken)
    {
        var items = await inventoryItemRepository.FindAllByRestaurantIdAsync(restaurantId, cancellationToken);
        var lines = new Dictionary<int, RecipeIngredient>();
        foreach (var line in data)
        {
            var item = items.FirstOrDefault(candidate => candidate.Id == line.InventoryItemId);
            if (item == null)
                return Result<List<RecipeIngredient>>.Failure(ProductInventoryError.IngredientNotFound,
                    localizer[nameof(ProductInventoryError.IngredientNotFound), line.InventoryItemId]);
            if (!line.QuantityRequired.IsPositive)
                return Result<List<RecipeIngredient>>.Failure(ProductInventoryError.InvalidData, localizer["IngredientQuantity"]);
            lines[item.Id] = new RecipeIngredient(item.Id, item.Name, line.QuantityRequired, item.Unit);
        }

        return Result<List<RecipeIngredient>>.Success(lines.Values.ToList());
    }
}
