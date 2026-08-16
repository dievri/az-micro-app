using AzMicroApp.Common;
using AzMicroApp.Gateway.Models;
using AzMicroApp.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

const string ServiceName = "gateway";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseStructuredJsonLogging(ServiceName);

// The Gateway is the only public service: plain HTTP/1.1+HTTP/2 on 8080.
var httpPort = int.Parse(Environment.GetEnvironmentVariable("GATEWAY_HTTP_PORT") ?? "8080");
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(httpPort));

// Needed by the gRPC client interceptor to read the current request's id.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<RequestIdClientInterceptor>();

// Downstream gRPC endpoints — hostnames/ports are fully env-driven so the same
// image works in Docker Compose and later in Azure Container Apps.
string GrpcAddress(string hostVar, string hostDefault, string portVar, string portDefault)
{
    var host = Environment.GetEnvironmentVariable(hostVar) ?? hostDefault;
    var port = Environment.GetEnvironmentVariable(portVar) ?? portDefault;
    return $"http://{host}:{port}";
}

builder.Services
    .AddGrpcClient<UserService.UserServiceClient>(o =>
        o.Address = new Uri(GrpcAddress("USERS_GRPC_HOST", "users", "USERS_GRPC_PORT", "50051")))
    .AddInterceptor<RequestIdClientInterceptor>();

builder.Services
    .AddGrpcClient<HotelService.HotelServiceClient>(o =>
        o.Address = new Uri(GrpcAddress("HOTELS_GRPC_HOST", "hotels", "HOTELS_GRPC_PORT", "50052")))
    .AddInterceptor<RequestIdClientInterceptor>();

builder.Services
    .AddGrpcClient<BookingService.BookingServiceClient>(o =>
        o.Address = new Uri(GrpcAddress("BOOKINGS_GRPC_HOST", "bookings", "BOOKINGS_GRPC_PORT", "50053")))
    .AddInterceptor<RequestIdClientInterceptor>();

builder.Services.AddOptionalOpenTelemetry(
    ServiceName, instrumentAspNetCore: true, instrumentGrpcClient: true);

var app = builder.Build();

app.UseRequestId();

// --- Endpoints -------------------------------------------------------------

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = ServiceName }));

// GET /booking/{id}: fetch the booking, then resolve its user and hotel and
// return the aggregated document.
app.MapGet("/booking/{bookingId}", async (
    string bookingId,
    BookingService.BookingServiceClient bookings,
    UserService.UserServiceClient users,
    HotelService.HotelServiceClient hotels,
    ILogger<Program> logger) =>
{
    try
    {
        var booking = await bookings.GetBookingAsync(new BookingRequest { BookingId = bookingId });
        var user = await users.GetUserAsync(new UserRequest { UserId = booking.UserId });
        var hotel = await hotels.GetHotelAsync(new HotelRequest { HotelId = booking.HotelId });

        return Results.Ok(Aggregate(booking, user, hotel));
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
    {
        logger.LogWarning("Aggregation failed for booking {BookingId}: {Detail}", bookingId, ex.Status.Detail);
        return Results.NotFound(new { error = ex.Status.Detail });
    }
});

// POST /booking: create a booking, then return the aggregated document.
app.MapPost("/booking", async (
    [FromBody] CreateBookingRequestDto req,
    BookingService.BookingServiceClient bookings,
    UserService.UserServiceClient users,
    HotelService.HotelServiceClient hotels,
    ILogger<Program> logger) =>
{
    try
    {
        var booking = await bookings.CreateBookingAsync(new CreateBookingRequest
        {
            UserId = req.UserId,
            HotelId = req.HotelId,
            CheckIn = req.CheckIn,
            CheckOut = req.CheckOut,
        });

        var user = await users.GetUserAsync(new UserRequest { UserId = booking.UserId });
        var hotel = await hotels.GetHotelAsync(new HotelRequest { HotelId = booking.HotelId });

        return Results.Created($"/booking/{booking.Id}", Aggregate(booking, user, hotel));
    }
    catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
    {
        logger.LogWarning("Create booking failed: {Detail}", ex.Status.Detail);
        return Results.BadRequest(new { error = ex.Status.Detail });
    }
});

app.Logger.LogInformation("{Service} listening on http://0.0.0.0:{Port}", ServiceName, httpPort);

app.Run();

static AggregatedBookingDto Aggregate(Booking booking, User user, Hotel hotel) => new(
    new BookingDto(booking.Id, booking.UserId, booking.HotelId,
        booking.CheckIn, booking.CheckOut, booking.Status),
    new UserDto(user.Id, user.Name, user.Email),
    new HotelDto(hotel.Id, hotel.Name, hotel.City, hotel.Country));

public partial class Program;
