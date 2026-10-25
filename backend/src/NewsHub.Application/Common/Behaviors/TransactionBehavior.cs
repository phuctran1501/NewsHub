using MediatR;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;

namespace NewsHub.Application.Common.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior tự động bọc Database Transaction xung quanh các Request có đánh dấu ITransactionalRequest.
/// </summary>
public class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Nếu request không đánh dấu ITransactionalRequest thì bỏ qua, chuyển tiếp cho handler kế tiếp
        if (request is not ITransactionalRequest)
        {
            return await next(cancellationToken);
        }

        var requestName = typeof(TRequest).Name;

        try
        {
            logger.LogInformation("Bắt đầu transaction cho request {RequestName}", requestName);

            var response = await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                return await next(ct);
            }, cancellationToken);

            logger.LogInformation("Commit transaction thành công cho request {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transaction cho request {RequestName} thất bại và đã được rollback: {ErrorMessage}",
                requestName, ex.Message);
            throw;
        }
    }
}
