namespace NewsHub.Application.Common.Interfaces;

/// <summary>
/// Cung cấp CorrelationId hiện tại của request để phục vụ tracing và logging xuyên suốt pipeline.
/// </summary>
public interface ICorrelationIdProvider
{
    string GetCorrelationId();
}
