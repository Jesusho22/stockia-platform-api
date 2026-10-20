using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.Subscription.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class PlanRepository(AppDbContext context) : BaseRepository<Plan>(context), IPlanRepository
{
    public Task<Plan?> FindFreePlanAsync(CancellationToken cancellationToken = default) =>
        Context.Set<Plan>().OrderBy(plan => plan.MonthlyPrice).FirstOrDefaultAsync(cancellationToken);
}
