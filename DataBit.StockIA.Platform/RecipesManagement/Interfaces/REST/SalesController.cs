using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.RecipesManagement.Application.Services;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Commands;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.Queries;
using DataBit.StockIA.Platform.RecipesManagement.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.RecipesManagement.Interfaces.REST;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Sales history and sale registration")]
public class SalesController(
    ISaleCommandService saleCommandService,
    ISaleQueryService saleQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    /// <summary>Sales newest first. Optional from/to (yyyy-MM-dd, restaurant time zone).</summary>
    [HttpGet]
    [EndpointSummary("List the sales")]
    [EndpointName("GetSales")]
    public async Task<IActionResult> GetAll([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var sales = await saleQueryService.Handle(new GetSalesByRestaurantIdQuery(RestaurantId, from, to), cancellationToken);
        return Ok(sales.Select(SaleResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a sale")]
    [EndpointName("GetSaleById")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var sale = await saleQueryService.Handle(new GetSaleByIdQuery(id, RestaurantId), cancellationToken);
        return sale == null ? NotFound() : Ok(SaleResourceFromEntityAssembler.ToResourceFromEntity(sale));
    }

    /// <summary>
    ///     Sells a dish. 422 CannotSell (nothing is recorded and the stock does
    ///     not change) when a supply is missing, short or only available in
    ///     expired lots; details.shortages explains each one.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Register a sale")]
    [EndpointName("RegisterSale")]
    [ProducesResponseType(typeof(SaleResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register([FromBody] RegisterSaleResource resource, CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<SaleChannel>(resource.Channel, out var channel)) channel = SaleChannel.Pos;
        var result = await saleCommandService.Handle(new RegisterSaleCommand(RestaurantId, resource.RecipeId, resource.Quantity, channel),
            cancellationToken);
        return RecipesManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            sale => CreatedAtAction(nameof(GetById), new { id = sale.Id }, SaleResourceFromEntityAssembler.ToResourceFromEntity(sale)));
    }

    /// <summary>Voids a confirmed sale; the supplies used are not returned to stock.</summary>
    [HttpPost("{id:int}/void")]
    [EndpointSummary("Void a sale")]
    [EndpointName("VoidSale")]
    public async Task<IActionResult> Void([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await saleCommandService.Handle(new VoidSaleCommand(id, RestaurantId), cancellationToken);
        return RecipesManagementActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            sale => Ok(SaleResourceFromEntityAssembler.ToResourceFromEntity(sale)));
    }
}
