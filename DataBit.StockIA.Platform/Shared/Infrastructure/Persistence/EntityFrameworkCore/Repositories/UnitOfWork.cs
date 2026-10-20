using Microsoft.EntityFrameworkCore.Storage;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Commits the change set with AppDbContext.SaveChangesAsync. Nested
///     BeginTransactionAsync calls reuse the transaction already open.
/// </summary>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction != null) return new NestedTransaction();
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransaction(transaction);
    }

    private sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    /// <summary>The outer transaction decides; inner commit/rollback do nothing.</summary>
    private sealed class NestedTransaction : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
