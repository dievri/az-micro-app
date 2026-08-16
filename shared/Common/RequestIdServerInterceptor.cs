using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Common;

/// <summary>
/// gRPC server-side interceptor. Extracts the correlation id from incoming
/// metadata (x-request-id) — or generates one if absent — and pushes it into
/// the logging scope so every log line produced while handling the call carries
/// request_id.
/// </summary>
public sealed class RequestIdServerInterceptor : Interceptor
{
    private readonly ILogger<RequestIdServerInterceptor> _logger;

    public RequestIdServerInterceptor(ILogger<RequestIdServerInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var requestId =
            context.RequestHeaders.GetValue(Correlation.MetadataKey)
            ?? Guid.NewGuid().ToString("N");

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [Correlation.LogPropertyName] = requestId
        }))
        {
            return await continuation(request, context);
        }
    }
}
