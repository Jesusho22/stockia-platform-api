using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.StockManagement.Application.Services;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.Queries;
using DataBit.StockIA.Platform.StockManagement.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.StockManagement.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.StockManagement.Interfaces.REST.Transform;

namespace DataBit.StockIA.Platform.StockManagement.Interfaces.REST;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Supplies (lots) of the restaurant")]
public class InventoryItemsController(
    IInventoryItemCommandService inventoryItemCommandService,
    IInventoryItemQueryService queryService,
    ICurrentUserAccessor currentUser,
    IRestaurantClock clock,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    [HttpGet]
    [EndpointSummary("List the supplies (newest first)")]
    [EndpointName("GetInventoryItems")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await queryService.Handle(new GetInventoryItemsByRestaurantIdQuery(RestaurantId), cancellationToken);
        return Ok(items.Select(item => InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today)));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a supply")]
    [EndpointName("GetInventoryItemById")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetInventoryItemByIdQuery(id, RestaurantId), cancellationToken);
        return item == null ? NotFound() : Ok(InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today));
    }

    [HttpPost]
    [EndpointSummary("Register a supply lot")]
    [EndpointName("CreateInventoryItem")]
    [ProducesResponseType(typeof(InventoryItemResource), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] SaveInventoryItemResource resource, CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<StorageType>(resource.StorageType, out var storageType)) return InvalidStorageType();
        var result = await inventoryItemCommandService.Handle(
            InventoryItemCommandFromResourceAssembler.ToCreateCommand(resource, storageType, RestaurantId), cancellationToken);
        return StockManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory, item =>
            CreatedAtAction(nameof(GetById), new { id = item.Id }, InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today)));
    }

    /// <summary>Edits a supply; the expiration date is recalculated from the shelf life.</summary>
    [HttpPut("{id:int}")]
    [EndpointSummary("Edit a supply")]
    [EndpointName("UpdateInventoryItem")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveInventoryItemResource resource,
        CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<StorageType>(resource.StorageType, out var storageType)) return InvalidStorageType();
        var result = await inventoryItemCommandService.Handle(
            InventoryItemCommandFromResourceAssembler.ToUpdateCommand(resource, storageType, id, RestaurantId), cancellationToken);
        return StockManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            item => Ok(InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item, clock.Today)));
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete a supply")]
    [EndpointName("DeleteInventoryItem")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await inventoryItemCommandService.Handle(new DeleteInventoryItemCommand(id, RestaurantId), cancellationToken);
        return StockManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }

    private BadRequestObjectResult InvalidStorageType() =>
        BadRequest(problemDetailsFactory.CreateProblemDetails(StatusCodes.Status400BadRequest, "InvalidStorageType",
            "The storage type must be AMBIENT, REFRIGERATED or FROZEN."));
}
