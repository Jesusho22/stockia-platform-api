using System.Net.Mime;
using DataBit.StockIA.Platform.AnalyticsDashboard.Application.Services;
using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.Queries;
using DataBit.StockIA.Platform.AnalyticsDashboard.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.AnalyticsDashboard.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBit.StockIA.Platform.AnalyticsDashboard.Interfaces.REST;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Business dashboard")]
public class DashboardController(IDashboardQueryService dashboardQueryService, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet("summary")]
    [EndpointSummary("KPIs of the restaurant")]
    [EndpointName("GetDashboardSummary")]
    [ProducesResponseType(typeof(DashboardSummaryResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        Ok(DashboardSummaryResourceFromValueAssembler.ToResource(
            await dashboardQueryService.Handle(new GetDashboardSummaryQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken)));

    /// <summary>
    ///     Estimated savings of the last 7 days: stock used before it expired and
    ///     less waste than the previous week. Administrator only (financial data).
    /// </summary>
    [HttpGet("weekly-savings")]
    [RequireRole("ADMIN")]
    [EndpointSummary("Weekly savings (Administrator)")]
    [EndpointName("GetWeeklySavings")]
    [ProducesResponseType(typeof(WeeklySavingsResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklySavings(CancellationToken cancellationToken) =>
        Ok(WeeklySavingsResourceFromValueAssembler.ToResource(
            await dashboardQueryService.Handle(new GetWeeklySavingsQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken)));
}