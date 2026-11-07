using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Queries;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Transform;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST;

/// <summary>Subscription type and Upgrade plan: Administrator only.</summary>
[Authorize]
[RequireRole("ADMIN")]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Subscription of the restaurant (Administrator)")]
public class SubscriptionsController(
    ISubscriptionCommandService subscriptionCommandService,
    ISubscriptionQueryService subscriptionQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("current")]
    [EndpointSummary("Get the current subscription")]
    [EndpointName("GetCurrentSubscription")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var subscription = await subscriptionQueryService.Handle(
            new GetSubscriptionByRestaurantIdQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken);
        return subscription == null ? NotFound() : Ok(SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription));
    }

    /// <summary>Chooses or changes the plan (simulated Stripe / PayPal checkout).</summary>
    [HttpPost]
    [EndpointSummary("Choose a plan")]
    [EndpointName("ChangePlan")]
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
