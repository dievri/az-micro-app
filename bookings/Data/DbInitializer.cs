using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Bookings.Data;

public static class DbInitializer
{
    /// <summary>
    /// Deterministic seed bookings (at least 5) so the demo is usable immediately
    /// after startup. Ids are stable so the README examples always work.
    /// </summary>
    private static readonly BookingEntity[] Seed =
    {
        new() { Id = "b1", UserId = "u1", HotelId = "h1", CheckIn = "2026-09-01", CheckOut = "2026-09-05", Status = "CONFIRMED" },
        new() { Id = "b2", UserId = "u2", HotelId = "h2", CheckIn = "2026-09-10", CheckOut = "2026-09-12", Status = "CONFIRMED" },
        new() { Id = "b3", UserId = "u3", HotelId = "h3", CheckIn = "2026-10-01", CheckOut = "2026-10-08", Status = "CONFIRMED" },
        new() { Id = "b4", UserId = "u1", HotelId = "h2", CheckIn = "2026-11-15", CheckOut = "2026-11-18", Status = "CONFIRMED" },
        new() { Id = "b5", UserId = "u2", HotelId = "h3", CheckIn = "2026-12-20", CheckOut = "2026-12-27", Status = "CANCELLED" },
    };

    /// <summary>
    /// Creates the table if it does not exist (per the lab requirement) and
    /// inserts the seed rows once. Retries a few times so it tolerates
    /// PostgreSQL still starting up in Docker Compose.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // EnsureCreated builds the schema when it is absent. Good enough
                // for a local lab; a real deployment would use migrations.
                await db.Database.EnsureCreatedAsync();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex,
                    "Database not ready (attempt {Attempt}/{Max}), retrying...",
                    attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        if (!await db.Bookings.AnyAsync())
        {
            db.Bookings.AddRange(Seed);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} bookings", Seed.Length);
        }
    }
}
