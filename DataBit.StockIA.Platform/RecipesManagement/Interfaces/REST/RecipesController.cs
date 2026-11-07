using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.RecipesManagement.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.RecipesManagement.Application.Services;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Queries;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Recipes (dishes and the supplies they consume)")]
public class RecipesController(
    IRecipeCommandService recipeCommandService,
    IRecipeQueryService recipeQueryService,
    ExternalStockService externalStockService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    [HttpGet]
    [EndpointSummary("List the recipes with the dish price (newest first)")]
    [EndpointName("GetRecipes")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var recipes = await recipeQueryService.Handle(new GetRecipesByRestaurantIdQuery(RestaurantId), cancellationToken);
        var prices = await externalStockService.PricesOf(RestaurantId, recipes, cancellationToken);
        return Ok(recipes.Select((recipe, index) => RecipeResourceFromEntityAssembler.ToResourceFromEntity(recipe, prices[index])));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a recipe")]
    [EndpointName("GetRecipeById")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var recipe = await recipeQueryService.Handle(new GetRecipeByIdQuery(id, RestaurantId), cancellationToken);
        if (recipe == null) return NotFound();
        return Ok(RecipeResourceFromEntityAssembler.ToResourceFromEntity(recipe, await PriceOf(recipe, cancellationToken)));
    }

    [HttpPost]
    [EndpointSummary("Create a recipe")]
    [EndpointName("CreateRecipe")]
    public async Task<IActionResult> Create([FromBody] SaveRecipeResource resource, CancellationToken cancellationToken)
    {
        var result = await recipeCommandService.Handle(RecipeCommandFromResourceAssembler.ToCreateCommand(resource, RestaurantId),
            cancellationToken);
        if (result.IsFailure) return RecipesManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory, _ => Ok());
        var recipe = result.Value!;
        return CreatedAtAction(nameof(GetById), new { id = recipe.Id },
            RecipeResourceFromEntityAssembler.ToResourceFromEntity(recipe, await PriceOf(recipe, cancellationToken)));
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Edit a recipe")]
    [EndpointName("UpdateRecipe")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveRecipeResource resource, CancellationToken cancellationToken)
    {
        var result = await recipeCommandService.Handle(RecipeCommandFromResourceAssembler.ToUpdateCommand(resource, id, RestaurantId),
            cancellationToken);
        if (result.IsFailure) return RecipesManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory, _ => Ok());
        var recipe = result.Value!;
        return Ok(RecipeResourceFromEntityAssembler.ToResourceFromEntity(recipe, await PriceOf(recipe, cancellationToken)));
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete a recipe")]
    [EndpointName("DeleteRecipe")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await recipeCommandService.Handle(new DeleteRecipeCommand(id, RestaurantId), cancellationToken);
        return RecipesManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }

    private async Task<decimal> PriceOf(Recipe recipe, CancellationToken cancellationToken) =>
        (await externalStockService.PricesOf(RestaurantId, [recipe], cancellationToken))[0];
}
