using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;

namespace AzMicroApp.Common;

/// <summary>
/// gRPC client-side interceptor used by the Gateway. It copies the current
/// request's correlation id (captured by <see cref="RequestIdMiddleware"/>)
/// into the outgoing gRPC metadata as x-request-id, so the internal services
/// can log against the same id.
/// </summary>
public sealed class RequestIdClientInterceptor : Interceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestIdClientInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var requestId = _httpContextAccessor.HttpContext?.GetRequestId();
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            var metadata = context.Options.Headers ?? new Metadata();
            if (metadata.GetValue(Correlation.MetadataKey) is null)
            {
                metadata.Add(Correlation.MetadataKey, requestId);
            }

            var newOptions = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, newOptions);
        }

        return continuation(request, context);
    }
}
