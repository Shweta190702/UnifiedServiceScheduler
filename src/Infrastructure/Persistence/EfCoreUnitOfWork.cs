using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using UnifiedServiceScheduler.Domain.Repositories;

namespace UnifiedServiceScheduler.Infrastructure.Persistence;

/// <summary>
/// EF Core Unit of Work implementation.
/// Coordinates transactions across multiple aggregate repositories and handles
/// concurrency conflicts (DbUpdateConcurrencyException).
/// </summary>
public sealed class EfCoreUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly UnifiedServiceSchedulerDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public EfCoreUnitOfWork(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Persists all pending entity changes across all tracked aggregates.
    /// In EF Core, calling SaveChangesAsync within an ambient or explicit transaction
    /// guarantees all aggregate changes succeed or fail atomically.
    /// </summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Propagate concurrency conflict for application handler translation
            throw new ConcurrencyConflictException(
                "A concurrent reservation modified one or more scheduled resources before this booking could complete.",
                ex);
        }
    }

    /// <summary>
    /// Begins an explicit database transaction with the specified isolation level.
    /// Default: IsolationLevel.ReadCommitted (suitable when using SELECT ... FOR UPDATE),
    /// or IsolationLevel.Serializable for complete mathematical isolation.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
    {
        if (_currentTransaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _currentTransaction = await _context.Database.BeginTransactionAsync(isolationLevel, ct);
        return _currentTransaction;
    }

    /// <summary>
    /// Commits the active explicit transaction.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await _context.SaveChangesAsync(ct);
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Rolls back the active transaction and clears the EF Core change tracker
    /// to avoid stale entity state.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null)
            return;

        try
        {
            await _currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Executes a unit of work inside an EF Core execution strategy to support
    /// transient fault retries (e.g. database connection drops or deadlock retries).
    /// </summary>
    public async Task<TResult> ExecuteInExecutionStrategyAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(operation);
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception inner)
        : base(message, inner) { }
}
