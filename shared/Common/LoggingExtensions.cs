using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Common;

public static class LoggingExtensions
{
    /// <summary>
    /// Configures structured JSON logging for a service. Each log record carries
    /// a timestamp, level, message, the service name and (when available) the
    /// request id via logging scopes. This maps cleanly onto Azure Monitor /
    /// Log Analytics later without any code change.
    /// </summary>
    public static IHostBuilder UseStructuredJsonLogging(this IHostBuilder host, string serviceName)
    {
        return host.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
                {
                    Indented = false
                };
            });

            // Attach the service name to every log line via a static scope.
            logging.Configure(o =>
            {
                o.ActivityTrackingOptions =
                    Microsoft.Extensions.Logging.ActivityTrackingOptions.TraceId |
                    Microsoft.Extensions.Logging.ActivityTrackingOptions.SpanId;
            });
        });
    }
}
