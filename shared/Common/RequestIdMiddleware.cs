using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Common;

/// <summary>
/// Ensures every HTTP request has a correlation id. If the caller supplied one
/// via the X-Request-ID header it is reused; otherwise a new id is generated.
/// The id is echoed back on the response and pushed into the logging scope so
/// every log line for this request carries request_id.
/// </summary>
public sealed class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var requestId = context.Request.Headers[Correlation.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        // Make it available to downstream code (e.g. gRPC clients) and the response.
        context.Items[Correlation.LogPropertyName] = requestId;
        context.Response.Headers[Correlation.HeaderName] = requestId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [Correlation.LogPropertyName] = requestId
        }))
        {
            await _next(context);
        }
    }
}

public static class RequestIdMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestId(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestIdMiddleware>();

    /// <summary>Reads the correlation id captured by the middleware for the current request.</summary>
    public static string? GetRequestId(this HttpContext context) =>
        context.Items.TryGetValue(Correlation.LogPropertyName, out var v) ? v as string : null;
}
