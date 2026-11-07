using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

public interface IPlanRepository : IBaseRepository<Plan>
{
    Task<Plan?> FindFreePlanAsync(CancellationToken cancellationToken = default);
}
