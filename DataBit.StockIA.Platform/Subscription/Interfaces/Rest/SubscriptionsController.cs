using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Subscription.Application.CommandServices;
using DataBit.StockIA.Platform.Subscription.Application.QueryServices;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Commands;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Queries;
using DataBit.StockIA.Platform.Subscription.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Subscription.Interfaces.Rest;

/// <summary>Subscription type and Upgrade plan: Administrator only.</summary>
[Authorize]
[RequireRole("ADMIN")]
[ApiController]
[Route("api/v1/subscriptions")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Subscription of the restaurant (Administrator)")]
public class SubscriptionsController(
    ISubscriptionCommandService subscriptionCommandService,
    ISubscriptionQueryService subscriptionQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("current")]
    [SwaggerOperation(Summary = "Get the current subscription", OperationId = "GetCurrentSubscription")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var subscription = await subscriptionQueryService.Handle(
            new GetSubscriptionByRestaurantIdQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken);
        return subscription == null ? NotFound() : Ok(SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription));
    }

    /// <summary>Chooses or changes the plan (simulated Stripe / PayPal checkout).</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Choose a plan", OperationId = "ChangePlan")]
    public async Task<IActionResult> ChangePlan([FromBody] ChangePlanResource resource, CancellationToken cancellationToken)
    {
        if (!EnumCode.TryParse<PaymentMethod>(resource.PaymentMethod, out var paymentMethod))
            return BadRequest(problemDetailsFactory.CreateProblemDetails(StatusCodes.Status400BadRequest, "InvalidPaymentMethod",
                "The payment method must be STRIPE or PAYPAL."));

        var result = await subscriptionCommandService.Handle(
            new ChangePlanCommand(currentUser.CurrentRestaurantId!.Value, resource.PlanId, paymentMethod), cancellationToken);
        return SubscriptionActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            subscription => Ok(SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription)));
    }
}
