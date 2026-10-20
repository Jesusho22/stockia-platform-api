namespace DataBit.StockIA.Platform.Shared.Domain.Repositories;

/// <summary>
///     CRUD contract every bounded-context repository builds on.
/// </summary>
/// <typeparam name="TEntity">Aggregate managed by the repository.</typeparam>
public interface IBaseRepository<TEntity> where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
