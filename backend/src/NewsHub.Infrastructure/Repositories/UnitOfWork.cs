using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure.Persistence;

namespace NewsHub.Infrastructure.Repositories;

/// <summary>
/// Triển khai Unit of Work và Database Transaction bọc quanh NewsHubDbContext và Npgsql Execution Strategy.
/// </summary>
public class UnitOfWork : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly NewsHubDbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(NewsHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null || _dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Một database transaction đang hoạt động.");
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            throw new InvalidOperationException("Không có transaction nào đang hoạt động để commit.");
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        // Nếu đã có transaction đang chạy, thực thi trực tiếp để tránh lỗi lồng transaction
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            _currentTransaction = transaction;

            try
            {
                var result = await action(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch
                {
                    // Bỏ qua lỗi rollback thứ cấp để không che lỗi gốc (cần inject ILogger để ghi log).
                }
                throw;
            }
            finally
            {
                _currentTransaction = null;
            }
        });
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async ct =>
        {
            await action(ct);
            return true;
        }, cancellationToken);
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _currentTransaction = null;
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
