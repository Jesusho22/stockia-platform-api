using DataBit.StockIA.Platform.Dashboard.Interfaces.Rest.Resources;

namespace DataBit.StockIA.Platform.Dashboard.Application.QueryServices;

public interface IDashboardQueryService
{
    Task<DashboardSummaryResource> GetSummary(int restaurantId, CancellationToken cancellationToken);
}
