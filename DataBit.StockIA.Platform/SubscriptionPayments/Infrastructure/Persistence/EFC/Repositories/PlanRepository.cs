using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Persistence.EFC.Repositories;

public class PlanRepository(AppDbContext context) : BaseRepository<Plan>(context), IPlanRepository
{
    public Task<Plan?> FindFreePlanAsync(CancellationToken cancellationToken = default) =>
        Context.Set<Plan>().OrderBy(plan => plan.MonthlyPrice).FirstOrDefaultAsync(cancellationToken);
}
