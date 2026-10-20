using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.SalesOrder.Application.CommandServices;
using DataBit.StockIA.Platform.SalesOrder.Application.QueryServices;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Commands;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.Queries;
using DataBit.StockIA.Platform.SalesOrder.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SalesOrder.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.SalesOrder.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.SalesOrder.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/sales")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Sales history and sale registration")]
public class SalesController(
    ISaleCommandService saleCommandService,
    ISaleQueryService saleQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    /// <summary>Sales newest first. Optional from/to (yyyy-MM-dd, restaurant time zone).</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List the sales", OperationId = "GetSales")]
    public async Task<IActionResult> GetAll([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var sales = await saleQueryService.Handle(new GetSalesByRestaurantIdQuery(RestaurantId, from, to), cancellationToken);
        return Ok(sales.Select(SaleResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a sale", OperationId = "GetSaleById")]
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
    [SwaggerOperation(Summary = "Register a sale", OperationId = "RegisterSale")]
    [SwaggerResponse(StatusCodes.Status201Created, "Sale registered", typeof(SaleResource))]
    [SwaggerResponse(StatusCodes.Status422UnprocessableEntity, "Not enough stock or expired supplies")]
    public async Task<IActionResult> Register([FromBody] RegisterSaleResource resource, CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<SaleChannel>(resource.Channel, out var channel)) channel = SaleChannel.Pos;
        var result = await saleCommandService.Handle(new RegisterSaleCommand(RestaurantId, resource.RecipeId, resource.Quantity, channel),
            cancellationToken);
        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            sale => CreatedAtAction(nameof(GetById), new { id = sale.Id }, SaleResourceFromEntityAssembler.ToResourceFromEntity(sale)));
    }

    /// <summary>Voids a confirmed sale; the supplies used are not returned to stock.</summary>
    [HttpPost("{id:int}/void")]
    [SwaggerOperation(Summary = "Void a sale", OperationId = "VoidSale")]
    public async Task<IActionResult> Void([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await saleCommandService.Handle(new VoidSaleCommand(id, RestaurantId), cancellationToken);
        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            sale => Ok(SaleResourceFromEntityAssembler.ToResourceFromEntity(sale)));
    }
}
