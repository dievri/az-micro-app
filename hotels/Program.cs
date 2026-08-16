using AzMicroApp.Common;
using AzMicroApp.Hotels.Services;
using Grpc.HealthCheck;
using Grpc.Health.V1;

const string ServiceName = "hotels";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseStructuredJsonLogging(ServiceName);

var port = int.Parse(Environment.GetEnvironmentVariable("HOTELS_GRPC_PORT") ?? "50052");
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
builder.Services.AddGrpcHealthChecks();
builder.Services.AddOptionalOpenTelemetry(ServiceName);

var app = builder.Build();

app.MapGrpcService<HotelGrpcService>();
app.MapGrpcHealthChecksService();

var healthService = app.Services.GetRequiredService<HealthServiceImpl>();
healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);
healthService.SetStatus(ServiceName, HealthCheckResponse.Types.ServingStatus.Serving);

app.Logger.LogInformation("{Service} gRPC service listening on port {Port}", ServiceName, port);

app.Run();
