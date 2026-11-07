using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.NotificationsMessaging.Application.Services;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Commands;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.Queries;
using DataBit.StockIA.Platform.NotificationsMessaging.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.NotificationsMessaging.Interfaces.REST;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Stock, expiration and IoT alerts")]
public class AlertsController(IAlertCommandService alertCommandService,
    IAlertQueryService alertQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private int RestaurantId => currentUser.CurrentRestaurantId!.Value;

    [HttpGet]
    [EndpointSummary("List the alerts (newest first)")]
    [EndpointName("GetAlerts")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var alerts = await alertQueryService.Handle(new GetAlertsByRestaurantIdQuery(RestaurantId), cancellationToken);
        return Ok(alerts.Select(item => AlertResourceFromEntityAssembler.ToResourceFromEntity(item)));
    }

    [HttpPost]
    [EndpointSummary("Create an alert manually")]
    [EndpointName("CreateAlert")]
    public async Task<IActionResult> Create([FromBody] SaveAlertResource resource, CancellationToken cancellationToken)
    {
        if (!TryParse(resource, out var type, out var severity, out var channel)) return InvalidCodes();
        var result = await alertCommandService.Handle(new CreateAlertCommand(RestaurantId, type, severity, resource.Message, channel),
            cancellationToken);
        return NotificationsMessagingActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => StatusCode(StatusCodes.Status201Created, AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Edit an alert")]
    [EndpointName("UpdateAlert")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveAlertResource resource, CancellationToken cancellationToken)
    {
        if (!TryParse(resource, out var type, out var severity, out var channel)) return InvalidCodes();
        var result = await alertCommandService.Handle(new UpdateAlertCommand(id, RestaurantId, type, severity, resource.Message, channel),
            cancellationToken);
        return NotificationsMessagingActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete an alert")]
    [EndpointName("DeleteAlert")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new DeleteAlertCommand(id, RestaurantId), cancellationToken);
        return NotificationsMessagingActionResultAssembler.ToActionResult(result, problemDetailsFactory, NoContent);
    }

    /// <summary>Marks the alert as attended. 409 NotDelivered while a required channel is pending.</summary>
    [HttpPost("{id:int}/acknowledge")]
    [EndpointSummary("Mark an alert as attended")]
    [EndpointName("AcknowledgeAlert")]
    public async Task<IActionResult> Acknowledge([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new AcknowledgeAlertCommand(id, RestaurantId), cancellationToken);
        return NotificationsMessagingActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    /// <summary>Retries the delivery on the pending channel (critical alerts need WhatsApp and email).</summary>
    [HttpPost("{id:int}/deliveries")]
    [EndpointSummary("Retry the delivery of an alert")]
    [EndpointName("RetryAlertDelivery")]
    public async Task<IActionResult> RetryDelivery([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new RetryAlertDeliveryCommand(id, RestaurantId), cancellationToken);
        return NotificationsMessagingActionResultAssembler.ToActionResult(result, problemDetailsFactory,
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
})