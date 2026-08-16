using AzMicroApp.Common;
using AzMicroApp.Users.Services;
using Grpc.HealthCheck;
using Grpc.Health.V1;

const string ServiceName = "users";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseStructuredJsonLogging(ServiceName);

// Kestrel: listen for gRPC over HTTP/2 on the configured port.
var port = int.Parse(Environment.GetEnvironmentVariable("USERS_GRPC_PORT") ?? "50051");
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port, listen => listen.Protocols =
        Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<RequestIdServerInterceptor>();
});
builder.Services.AddSingleton<RequestIdServerInterceptor>();

// Standard gRPC health-checking protocol (used by container health checks).
builder.Services.AddGrpcHealthChecks();

builder.Services.AddOptionalOpenTelemetry(ServiceName);

var app = builder.Build();

app.MapGrpcService<UserGrpcService>();
app.MapGrpcHealthChecksService();

// Report the service as serving once wired up.
var healthService = app.Services.GetRequiredService<HealthServiceImpl>();
healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);
healthService.SetStatus(ServiceName, HealthCheckResponse.Types.ServingStatus.Serving);

app.Logger.LogInformation("{Service} gRPC service listening on port {Port}", ServiceName, port);

app.Run();
