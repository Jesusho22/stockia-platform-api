using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.Queries;
using DataBit.StockIA.Platform.AnalyticsDashboard.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.AnalyticsDashboard.Application.Services;

public interface IDashboardQueryService
{
    Task<DashboardSummary> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken);
    Task<WeeklySavings> Handle(GetWeeklySavingsQuery query, CancellationToken cancellationToken);
}