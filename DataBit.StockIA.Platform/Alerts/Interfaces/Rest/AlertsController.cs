using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Alerts.Application.CommandServices;
using DataBit.StockIA.Platform.Alerts.Application.QueryServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Queries;
using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Alerts.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Alerts.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Alerts.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/alerts")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Stock, expiration and IoT alerts")]
public class AlertsController(
    IAlertCommandService alertCommandService,
    IAlertQueryService alertQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    [HttpGet]
    [SwaggerOperation(Summary = "List the alerts (newest first)", OperationId = "GetAlerts")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var alerts = await alertQueryService.Handle(new GetAlertsByRestaurantIdQuery(RestaurantId), cancellationToken);
        return Ok(alerts.Select(item => AlertResourceFromEntityAssembler.ToResourceFromEntity(item)));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create an alert manually", OperationId = "CreateAlert")]
    public async Task<IActionResult> Create([FromBody] SaveAlertResource resource, CancellationToken cancellationToken)
    {
        if (!TryParse(resource, out var type, out var severity, out var channel)) return InvalidCodes();
        var result = await alertCommandService.Handle(new CreateAlertCommand(RestaurantId, type, severity, resource.Message, channel),
            cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => StatusCode(StatusCodes.Status201Created, AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Edit an alert", OperationId = "UpdateAlert")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveAlertResource resource, CancellationToken cancellationToken)
    {
        if (!TryParse(resource, out var type, out var severity, out var channel)) return InvalidCodes();
        var result = await alertCommandService.Handle(new UpdateAlertCommand(id, RestaurantId, type, severity, resource.Message, channel),
            cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete an alert", OperationId = "DeleteAlert")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new DeleteAlertCommand(id, RestaurantId), cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }

    /// <summary>Marks the alert as attended. 409 NotDelivered while a required channel is pending.</summary>
    [HttpPost("{id:int}/acknowledge")]
    [SwaggerOperation(Summary = "Mark an alert as attended", OperationId = "AcknowledgeAlert")]
    public async Task<IActionResult> Acknowledge([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new AcknowledgeAlertCommand(id, RestaurantId), cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    /// <summary>Retries the delivery on the pending channel (critical alerts need WhatsApp and email).</summary>
    [HttpPost("{id:int}/deliveries")]
    [SwaggerOperation(Summary = "Retry the delivery of an alert", OperationId = "RetryAlertDelivery")]
    public async Task<IActionResult> RetryDelivery([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new RetryAlertDeliveryCommand(id, RestaurantId), cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    private static bool TryParse(SaveAlertResource resource, out AlertType type, out AlertSeverity severity, out AlertChannel channel)
    {
        severity = default;
        channel = default;
        return EnumCode.TryParse(resource.Type, out type) && EnumCode.TryParse(resource.Severity, out severity) &&
               EnumCode.TryParse(resource.Channel, out channel);
    }

    private BadRequestObjectResult InvalidCodes() =>
        BadRequest(problemDetailsFactory.CreateProblemDetails(StatusCodes.Status400BadRequest, "InvalidAlert",
            "Use type LOW_STOCK, CRITICAL_STOCK, EXPIRING_SOON or IOT_FAULT; severity INFO, WARNING or CRITICAL; channel WHATSAPP or EMAIL."));
}
