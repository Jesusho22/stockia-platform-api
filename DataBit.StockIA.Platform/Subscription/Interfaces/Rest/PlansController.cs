using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Subscription.Application.QueryServices;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Queries;
using DataBit.StockIA.Platform.Subscription.Interfaces.Rest.Transform;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Subscription.Interfaces.Rest;

[ApiController]
[Route("api/v1/plans")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Plan catalog (public: the Landing Page can show it)")]
public class PlansController(ISubscriptionQueryService subscriptionQueryService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "List the plans", OperationId = "GetPlans")]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await subscriptionQueryService.Handle(new GetAllPlansQuery(), cancellationToken);
        return Ok(plans.Select(PlanResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
