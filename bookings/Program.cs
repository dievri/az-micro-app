using AzMicroApp.Bookings.Data;
using AzMicroApp.Bookings.Services;
using AzMicroApp.Common;
using Grpc.HealthCheck;
using Grpc.Health.V1;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

// Azure SQL connection string. Two modes, both driven by environment variables:
//   1. If SQL_CONNECTION_STRING is set, use it verbatim (e.g. local dev with a
//      full connection string, or SQL auth).
//   2. Otherwise assemble a passwordless connection string that authenticates
//      via the container's Managed Identity — no secrets anywhere. This is the
//      Azure production path.
var connString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connString))
{
    var b = new SqlConnectionStringBuilder
    {
        DataSource = Environment.GetEnvironmentVariable("SQL_SERVER")        // e.g. myserver.database.windows.net
                     ?? "localhost",
        InitialCatalog = Environment.GetEnvironmentVariable("SQL_DATABASE") ?? "bookings",
        Encrypt = true,
        // Passwordless: SqlClient acquires an Entra token for the container's
        // Managed Identity. No user/password is ever supplied.
        Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity,
    };
    connString = b.ConnectionString;
}

builder.Services.AddDbContext<BookingsDbContext>(options =>
    options.UseSqlServer(connString));

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
