using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.MlRecommendations.Application.Errors;
using DataBit.StockIA.Platform.MlRecommendations.Application.Services;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Repositories;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Services;
using DataBit.StockIA.Platform.MlRecommendations.Interfaces.ACL;
using DataBit.StockIA.Platform.MlRecommendations.Resources;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.ACL;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.StockManagement.Interfaces.ACL;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Internal.CommandServices;

/// <summary>
///     Menu Recommendation Engine (C4): purchase and menu recommendations from a
///     demand forecast (RecommendationPolicy). The
///     pending recommendations of the previous forecast are replaced; applied
///     ones are kept as history.
/// </summary>
public class RecommendationCommandService(
    IRecommendationRepository recommendationRepository,
    IMlRecommendationsContextFacade mlRecommendationsContextFacade,
    IRecipesManagementContextFacade recipesManagementContextFacade,
    IStockManagementContextFacade stockManagementContextFacade,
    IRestaurantClock clock,
    IUnitOfWork unitOfWork,
    IStringLocalizer<MlRecommendationsMessages> localizer) : IRecommendationCommandService
{
    public async Task<IReadOnlyList<Recommendation>> Handle(GenerateRecommendationsCommand command, CancellationToken cancellationToken)
    {
        var forecast = await mlRecommendationsContextFacade.GetForecast(command.RestaurantId, command.DemandForecastId, cancellationToken);
        if (forecast == null) return [];

        var recipes = await recipesManagementContextFacade.GetActiveRecipes(command.RestaurantId, cancellationToken);
        var inventory = await stockManagementContextFacade.GetInventory(command.RestaurantId, cancellationToken);
        var today = clock.Today;
        var lastWeek = await recipesManagementContextFacade.GetDailyUnitsByDish(command.RestaurantId, today.AddDays(-7), today.AddDays(-1),
            cancellationToken);

        var dishes = forecast.Dishes.Select(projection =>
        {
            var recipe = recipes.FirstOrDefault(candidate => candidate.DishName == projection.DishName);
            var ingredients = recipe?.Ingredients
                .Select(line => new IngredientNeed(line.ProductKey, line.ItemName, line.Unit, line.QuantityRequired)).ToList() ?? [];
            return new DishPlan(projection.DishName, projection.TotalUnits,
                lastWeek.Where(row => row.DishName == projection.DishName).Sum(row => row.Units), ingredients);
        }).ToList();
        var stock = inventory.GroupBy(lot => lot.ProductKey).Select(group => new ProductStock(group.Key, group.First().Name,
            group.First().Unit, group.Where(lot => lot.Status != "EXPIRED").Sum(lot => lot.Quantity))).ToList();

        foreach (var pending in await recommendationRepository.FindPendingByRestaurantIdAsync(command.RestaurantId, cancellationToken))
            recommendationRepository.Remove(pending);

        var created = RecommendationPolicy.Propose(dishes, stock, forecast.To)
            .Select(proposal => new Recommendation(command.RestaurantId, proposal.Type, proposal.Message, proposal.ExpectedImpact, forecast.ForecastId))
            .ToList();
        foreach (var recommendation in created) await recommendationRepository.AddAsync(recommendation, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return created;
    }

    public async Task<Result<Recommendation>> Handle(ApplyRecommendationCommand command, CancellationToken cancellationToken)
    {
        var recommendation = await recommendationRepository.FindByIdAsync(command.RecommendationId, cancellationToken);
        if (recommendation == null || recommendation.RestaurantId != command.RestaurantId)
            return Result<Recommendation>.Failure(MlRecommendationsError.RecommendationNotFound, localizer[nameof(MlRecommendationsError.RecommendationNotFound)]);
        recommendation.Apply();
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Recommendation>.Success(recommendation);
    }
}
