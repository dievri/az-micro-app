using AzMicroApp.Bookings.Data;
using AzMicroApp.Bookings.Messaging;
using AzMicroApp.Bookings.Services;
using AzMicroApp.Protos;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AzMicroApp.Tests;

public class BookingsServiceTests
{
    private static BookingsDbContext NewDb(string name)
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;
        return new BookingsDbContext(options);
    }

    // No-op publisher: null Service Bus client means publish is skipped.
    private static BookingEventPublisher NoOpPublisher() =>
        new(client: null, NullLogger<BookingEventPublisher>.Instance);

    [Fact]
    public async Task GetBooking_ReturnsExisting()
    {
        using var db = NewDb(nameof(GetBooking_ReturnsExisting));
        db.Bookings.Add(new BookingEntity
        {
            Id = "b1", UserId = "u1", HotelId = "h1",
            CheckIn = "2026-09-01", CheckOut = "2026-09-05", Status = "CONFIRMED"
        });
        await db.SaveChangesAsync();

        var svc = new BookingGrpcService(db, NoOpPublisher(), NullLogger<BookingGrpcService>.Instance);
        var booking = await svc.GetBooking(new BookingRequest { BookingId = "b1" }, TestServerCallContext.Create());

        Assert.Equal("b1", booking.Id);
        Assert.Equal("u1", booking.UserId);
        Assert.Equal("h1", booking.HotelId);
    }

    [Fact]
    public async Task GetBooking_Unknown_ThrowsNotFound()
    {
        using var db = NewDb(nameof(GetBooking_Unknown_ThrowsNotFound));
        var svc = new BookingGrpcService(db, NoOpPublisher(), NullLogger<BookingGrpcService>.Instance);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            svc.GetBooking(new BookingRequest { BookingId = "missing" }, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_PersistsAndReturns()
    {
        using var db = NewDb(nameof(CreateBooking_PersistsAndReturns));
        var svc = new BookingGrpcService(db, NoOpPublisher(), NullLogger<BookingGrpcService>.Instance);

        var created = await svc.CreateBooking(new CreateBookingRequest
        {
            UserId = "u2", HotelId = "h3", CheckIn = "2026-10-01", CheckOut = "2026-10-04"
        }, TestServerCallContext.Create());

        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.Equal("CONFIRMED", created.Status);
        Assert.Equal(1, await db.Bookings.CountAsync());
    }

    [Fact]
    public async Task CreateBooking_MissingFields_ThrowsInvalidArgument()
    {
        using var db = NewDb(nameof(CreateBooking_MissingFields_ThrowsInvalidArgument));
        var svc = new BookingGrpcService(db, NoOpPublisher(), NullLogger<BookingGrpcService>.Instance);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            svc.CreateBooking(new CreateBookingRequest { UserId = "u2" }, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }
}
