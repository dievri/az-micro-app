using AzMicroApp.Bookings.Data;
using AzMicroApp.Bookings.Services;
using AzMicroApp.Common;
using Grpc.HealthCheck;
using Grpc.Health.V1;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Trace;

const string ServiceName = "bookings";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseStructuredJsonLogging(ServiceName);

var port = int.Parse(Environment.GetEnvironmentVariable("BOOKINGS_GRPC_PORT") ?? "50053");
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port, listen => listen.Protocols =
        Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// PostgreSQL connection string assembled purely from environment variables.
var connString = new NpgsqlConnectionStringBuilder
{
    Host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost",
    Port = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"),
    Database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "bookings",
    Username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres",
    Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres",
}.ConnectionString;

builder.Services.AddDbContext<BookingsDbContext>(options =>
    options.UseNpgsql(connString));

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<RequestIdServerInterceptor>();
});
builder.Services.AddSingleton<RequestIdServerInterceptor>();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddOptionalOpenTelemetry(
    ServiceName,
    configureExtra: tracing => tracing.AddEntityFrameworkCoreInstrumentation());

var app = builder.Build();

app.MapGrpcService<BookingGrpcService>();
app.MapGrpcHealthChecksService();

var healthService = app.Services.GetRequiredService<HealthServiceImpl>();
healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);
healthService.SetStatus(ServiceName, HealthCheckResponse.Types.ServingStatus.Serving);

// Create the table if missing and seed deterministic data.
await DbInitializer.InitializeAsync(app.Services, app.Logger);

app.Logger.LogInformation("{Service} gRPC service listening on port {Port}", ServiceName, port);

app.Run();
