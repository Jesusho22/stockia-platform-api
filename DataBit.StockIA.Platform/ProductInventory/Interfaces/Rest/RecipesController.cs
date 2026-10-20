using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;
using DataBit.StockIA.Platform.ProductInventory.Application.QueryServices;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Queries;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/recipes")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Recipes (dishes and the supplies they consume)")]
public class RecipesController(
    IRecipeCommandService recipeCommandService,
    IProductInventoryQueryService queryService,
    ICurrentUserAccessor currentUser,
    IRestaurantClock clock,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    [HttpGet]
    [SwaggerOperation(Summary = "List the recipes with the dish price (newest first)", OperationId = "GetRecipes")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await queryService.Handle(new GetInventoryItemsByRestaurantIdQuery(RestaurantId), cancellationToken);
        var recipes = await queryService.Handle(new GetRecipesByRestaurantIdQuery(RestaurantId), cancellationToken);
        return Ok(recipes.Select(recipe => RecipeResourceFromEntityAssembler.ToResourceFromEntity(recipe, items, clock.Today)));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a recipe", OperationId = "GetRecipeById")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var recipe = await queryService.Handle(new GetRecipeByIdQuery(id, RestaurantId), cancellationToken);
        if (recipe == null) return NotFound();
        var items = await queryService.Handle(new GetInventoryItemsByRestaurantIdQuery(RestaurantId), cancellationToken);
        return Ok(RecipeResourceFromEntityAssembler.ToResourceFromEntity(recipe, items, clock.Today));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a recipe", OperationId = "CreateRecipe")]
    public async Task<IActionResult> Create([FromBody] SaveRecipeResource resource, CancellationToken cancellationToken)
    {
        var result = await recipeCommandService.Handle(RecipeCommandFromResourceAssembler.ToCreateCommand(resource, RestaurantId),
            cancellationToken);
        if (result.IsFailure) return ProductInventoryActionResultAssembler.ToActionResult(result, problemDetailsFactory, _ => Ok());
        var items = await queryService.Handle(new GetInventoryItemsByRestaurantIdQuery(RestaurantId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id },
            RecipeResourceFromEntityAssembler.ToResourceFromEntity(result.Value, items, clock.Today));
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Edit a recipe", OperationId = "UpdateRecipe")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveRecipeResource resource, CancellationToken cancellationToken)
    {
        var result = await recipeCommandService.Handle(RecipeCommandFromResourceAssembler.ToUpdateCommand(resource, id, RestaurantId),
            cancellationToken);
        if (result.IsFailure) return ProductInventoryActionResultAssembler.ToActionResult(result, problemDetailsFactory, _ => Ok());
        var items = await queryService.Handle(new GetInventoryItemsByRestaurantIdQuery(RestaurantId), cancellationToken);
        return Ok(RecipeResourceFromEntityAssembler.ToResourceFromEntity(result.Value!, items, clock.Today));
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete a recipe", OperationId = "DeleteRecipe")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await recipeCommandService.Handle(new DeleteRecipeCommand(id, RestaurantId), cancellationToken);
        return ProductInventoryActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }
}
