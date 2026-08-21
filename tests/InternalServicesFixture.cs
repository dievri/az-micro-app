using AzMicroApp.Bookings.Data;
using AzMicroApp.Bookings.Services;
using AzMicroApp.Hotels.Services;
using AzMicroApp.Users.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AzMicroApp.Tests;

/// <summary>
/// Hosts the three real internal gRPC services (Users, Hotels, Bookings) on a
/// single in-process ASP.NET Core TestServer over HTTP/2. Bookings uses EF Core
/// InMemory so no PostgreSQL is required. This is what the integration test
/// drives to prove the Gateway -> Booking -> User -> Hotel aggregation path.
/// </summary>
public sealed class InternalServicesFixture : IAsyncLifetime
{
    public TestServer Server { get; private set; } = default!;
    public HttpClient Http { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddGrpc();
                services.AddDbContext<BookingsDbContext>(o =>
                    o.UseInMemoryDatabase("integration-bookings"));
                // No-op publisher for tests (no Service Bus client).
                services.AddSingleton(sp => new AzMicroApp.Bookings.Messaging.BookingEventPublisher(
                    client: null,
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzMicroApp.Bookings.Messaging.BookingEventPublisher>>()));
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<UserGrpcService>();
                    endpoints.MapGrpcService<HotelGrpcService>();
                    endpoints.MapGrpcService<BookingGrpcService>();
                });
            });
        });

        var host = await builder.StartAsync();
        Server = host.GetTestServer();

        // Seed one deterministic booking for the aggregation test.
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            db.Bookings.Add(new BookingEntity
            {
                Id = "b1", UserId = "u1", HotelId = "h1",
                CheckIn = "2026-09-01", CheckOut = "2026-09-05", Status = "CONFIRMED"
            });
            await db.SaveChangesAsync();
        }

        Http = Server.CreateClient();
    }

    public Task DisposeAsync()
    {
        Http?.Dispose();
        Server?.Dispose();
        return Task.CompletedTask;
    }
}
