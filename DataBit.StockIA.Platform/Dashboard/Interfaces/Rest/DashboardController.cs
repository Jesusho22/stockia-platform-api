using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Dashboard.Application.QueryServices;
using DataBit.StockIA.Platform.Dashboard.Interfaces.Rest.Resources;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using Swashbuckle.AspNetCore.Annotations;

namespace DataBit.StockIA.Platform.Dashboard.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/dashboard")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Business dashboard")]
public class DashboardController(IDashboardQueryService dashboardQueryService, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet("summary")]
    [SwaggerOperation(Summary = "KPIs of the restaurant", OperationId = "GetDashboardSummary")]
    [SwaggerResponse(StatusCodes.Status200OK, "Summary", typeof(DashboardSummaryResource))]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        Ok(await dashboardQueryService.GetSummary(currentUser.CurrentRestaurantId!.Value, cancellationToken));
}
