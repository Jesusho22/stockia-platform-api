using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.ProductInventory.Application.CommandServices;
using DataBit.StockIA.Platform.ProductInventory.Application.QueryServices;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Commands;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.Queries;
using DataBit.StockIA.Platform.ProductInventory.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.ProductInventory.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/inventory-items")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Supplies (lots) of the restaurant")]
public class InventoryItemsController(
    IInventoryItemCommandService inventoryItemCommandService,
    IProductInventoryQueryService queryService,
    ICurrentUserAccessor currentUser,
    IRestaurantClock clock,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    [HttpGet]
    [SwaggerOperation(Summary = "List the supplies (newest first)", OperationId = "GetInventoryItems")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await queryService.Handle(new GetInventoryItemsByRestaurantIdQuery(RestaurantId), cancellationToken);
        return Ok(items.Select(item => InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today)));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a supply", OperationId = "GetInventoryItemById")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetInventoryItemByIdQuery(id, RestaurantId), cancellationToken);
        return item == null ? NotFound() : Ok(InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Register a supply lot", OperationId = "CreateInventoryItem")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(InventoryItemResource))]
    public async Task<IActionResult> Create([FromBody] SaveInventoryItemResource resource, CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<StorageType>(resource.StorageType, out var storageType)) return InvalidStorageType();
        var result = await inventoryItemCommandService.Handle(
            InventoryItemCommandFromResourceAssembler.ToCreateCommand(resource, storageType, RestaurantId), cancellationToken);
        return ProductInventoryActionResultAssembler.ToActionResult(result, problemDetailsFactory, item =>
            CreatedAtAction(nameof(GetById), new { id = item.Id }, InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today)));
    }

    /// <summary>Edits a supply; the expiration date is recalculated from the shelf life.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Edit a supply", OperationId = "UpdateInventoryItem")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveInventoryItemResource resource,
        CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<StorageType>(resource.StorageType, out var storageType)) return InvalidStorageType();
        var result = await inventoryItemCommandService.Handle(
            InventoryItemCommandFromResourceAssembler.ToUpdateCommand(resource, storageType, id, RestaurantId), cancellationToken);
        return ProductInventoryActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            item => Ok(InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today)));
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete a supply", OperationId = "DeleteInventoryItem")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await inventoryItemCommandService.Handle(new DeleteInventoryItemCommand(id, RestaurantId), cancellationToken);
        return ProductInventoryActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }

    private BadRequestObjectResult InvalidStorageType() =>
        BadRequest(problemDetailsFactory.CreateProblemDetails(StatusCodes.Status400BadRequest, "InvalidStorageType",
            "The storage type must be AMBIENT, REFRIGERATED or FROZEN."));
}
