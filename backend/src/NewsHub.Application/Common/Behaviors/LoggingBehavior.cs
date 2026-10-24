using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;

namespace NewsHub.Application.Common.Behaviors;

/// <summary>
/// Pipeline Behavior tự động đo lường thời gian thực thi, ghi log thông tin request
/// và cảnh báo khi request vượt quá ngưỡng thời gian cho phép (500ms).
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long LongRunningThresholdMilliseconds = 500;
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _correlationIdProvider.GetCorrelationId();

        _logger.LogInformation(
            "Handling {RequestName} [CorrelationId: {CorrelationId}]",
            requestName,
            correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            if (elapsedMilliseconds > LongRunningThresholdMilliseconds)
            {
                _logger.LogWarning(
                    "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) [CorrelationId: {CorrelationId}]",
                    requestName,
                    elapsedMilliseconds,
                    correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMilliseconds} ms [CorrelationId: {CorrelationId}]",
                    requestName,
                    elapsedMilliseconds,
                    correlationId);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request failure {RequestName} after {ElapsedMilliseconds} ms [CorrelationId: {CorrelationId}]: {ErrorMessage}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                ex.Message);

            throw;
        }
    }
}
