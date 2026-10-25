using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure.Persistence;

namespace NewsHub.Infrastructure.Repositories;

/// <summary>
/// Triển khai Unit of Work và Database Transaction bọc quanh NewsHubDbContext và Npgsql Execution Strategy.
/// </summary>
public class UnitOfWork(
    NewsHubDbContext dbContext,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        // Nếu đã có transaction đang chạy, thực thi trực tiếp để tránh lỗi lồng transaction
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken);
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await action(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    logger.LogWarning(rollbackEx, "Gặp lỗi thứ cấp khi rollback transaction: {ErrorMessage}", rollbackEx.Message);
                }

                throw;
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
}
