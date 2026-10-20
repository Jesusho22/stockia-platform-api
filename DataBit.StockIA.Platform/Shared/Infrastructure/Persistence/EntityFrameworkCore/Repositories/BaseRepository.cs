using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     CRUD shared by every repository over the same AppDbContext.
/// </summary>
/// <typeparam name="TEntity">Aggregate managed by the repository.</typeparam>
public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext Context;

    protected BaseRepository(AppDbContext context)
    {
        Context = context;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public virtual async Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }

    public async Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().ToListAsync(cancellationToken);
    }
}
