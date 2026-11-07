using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Queries;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Transform;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Plan catalog (public: the Landing Page can show it)")]
public class PlansController(ISubscriptionQueryService subscriptionQueryService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary("List the plans")]
    [EndpointName("GetPlans")]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await subscriptionQueryService.Handle(new GetAllPlansQuery(), cancellationToken);
        return Ok(plans.Select(PlanResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
