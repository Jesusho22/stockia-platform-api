namespace DataBit.StockIA.Platform.Shared.Domain.Repositories;

/// <summary>
///     Commits the change set of the request. Every repository writes through
///     the same AppDbContext, so one command commits atomically.
/// </summary>
public interface IUnitOfWork
{
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Explicit transaction spanning several CompleteAsync calls (e.g. sign-up
    ///     creates the restaurant, the owner and the subscription; a sale saves
    ///     itself and the stock deduction).
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Persistence transaction without EF Core types in the application layer.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
