using AzMicroApp.Protos;
using Grpc.Net.Client;
using Xunit;

namespace AzMicroApp.Tests;

/// <summary>
/// End-to-end integration test of the aggregation path:
///   (Gateway logic) -> BookingService.GetBooking
///                   -> UserService.GetUser
///                   -> HotelService.GetHotel
/// against the three real internal services hosted in-process.
/// </summary>
public class GatewayAggregationIntegrationTests : IClassFixture<InternalServicesFixture>
{
    private readonly InternalServicesFixture _fixture;

    public GatewayAggregationIntegrationTests(InternalServicesFixture fixture) => _fixture = fixture;

    private GrpcChannel Channel() => GrpcChannel.ForAddress(
        _fixture.Server.BaseAddress,
        new GrpcChannelOptions { HttpHandler = _fixture.Server.CreateHandler() });

    [Fact]
    public async Task Aggregation_ResolvesBookingUserAndHotel()
    {
        using var channel = Channel();
        var bookings = new BookingService.BookingServiceClient(channel);
        var users = new UserService.UserServiceClient(channel);
        var hotels = new HotelService.HotelServiceClient(channel);

        // This mirrors exactly what the Gateway does for GET /booking/{id}.
        var booking = await bookings.GetBookingAsync(new BookingRequest { BookingId = "b1" });
        var user = await users.GetUserAsync(new UserRequest { UserId = booking.UserId });
        var hotel = await hotels.GetHotelAsync(new HotelRequest { HotelId = booking.HotelId });

        Assert.Equal("b1", booking.Id);
        Assert.Equal("u1", user.Id);
        Assert.Equal("Alice Johnson", user.Name);
        Assert.Equal("h1", hotel.Id);
        Assert.Equal("Grand Riverside", hotel.Name);
    }

    [Fact]
    public async Task CreateThenGet_RoundTrips()
    {
        using var channel = Channel();
        var bookings = new BookingService.BookingServiceClient(channel);

        var created = await bookings.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = "u2", HotelId = "h2", CheckIn = "2026-11-01", CheckOut = "2026-11-03"
        });

        var fetched = await bookings.GetBookingAsync(new BookingRequest { BookingId = created.Id });

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("u2", fetched.UserId);
        Assert.Equal("h2", fetched.HotelId);
    }
}
