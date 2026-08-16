using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AzMicroApp.Common;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing that is fully driven by environment variables.
    /// Tracing is only enabled when OTEL_ENABLED=true (or 1). When disabled the
    /// application still runs normally with zero exporter overhead.
    ///
    /// The OTLP endpoint is read from the standard OTEL_EXPORTER_OTLP_ENDPOINT
    /// variable, so no Azure-specific exporter is hard-coded.
    /// </summary>
    public static IServiceCollection AddOptionalOpenTelemetry(
        this IServiceCollection services,
        string serviceName,
        bool instrumentAspNetCore = false,
        bool instrumentGrpcClient = false,
        Action<TracerProviderBuilder>? configureExtra = null)
    {
        var enabled = IsEnabled(Environment.GetEnvironmentVariable("OTEL_ENABLED"));
        if (!enabled)
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                if (instrumentAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }
                if (instrumentGrpcClient)
                {
                    tracing.AddGrpcClientInstrumentation();
                    tracing.AddHttpClientInstrumentation();
                }

                configureExtra?.Invoke(tracing);

                // OTLP endpoint / protocol come from OTEL_EXPORTER_OTLP_* env vars.
                tracing.AddOtlpExporter();
            });

        return services;
    }

    private static bool IsEnabled(string? value) =>
        value is not null &&
        (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
}
