namespace NewsHub.Application.Common.Interfaces;

/// <summary>
/// Interface quản lý Unit of Work và Database Transaction, đảm bảo tính toàn vẹn ACID.
/// Sử dụng ExecuteInTransactionAsync để tương thích an toàn với EF Core Retrying Execution Strategy.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default);
}
