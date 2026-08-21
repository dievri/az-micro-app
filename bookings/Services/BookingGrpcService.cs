using AzMicroApp.Bookings.Data;
using AzMicroApp.Bookings.Messaging;
using AzMicroApp.Common;
using AzMicroApp.Protos;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Bookings.Services;

public sealed class BookingGrpcService : BookingService.BookingServiceBase
{
    private readonly BookingsDbContext _db;
    private readonly BookingEventPublisher _publisher;
    private readonly ILogger<BookingGrpcService> _logger;

    public BookingGrpcService(
        BookingsDbContext db,
        BookingEventPublisher publisher,
        ILogger<BookingGrpcService> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public override async Task<Booking> GetBooking(BookingRequest request, ServerCallContext context)
    {
        _logger.LogInformation("GetBooking called for booking_id={BookingId}", request.BookingId);

        var entity = await _db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, context.CancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Booking not found: {BookingId}", request.BookingId);
            throw new RpcException(new Status(
                StatusCode.NotFound, $"Booking '{request.BookingId}' not found"));
        }

        return ToProto(entity);
    }

    public override async Task<Booking> CreateBooking(CreateBookingRequest request, ServerCallContext context)
    {
        _logger.LogInformation(
            "CreateBooking called: user_id={UserId} hotel_id={HotelId}",
            request.UserId, request.HotelId);

        if (string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.HotelId) ||
            string.IsNullOrWhiteSpace(request.CheckIn) ||
            string.IsNullOrWhiteSpace(request.CheckOut))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "user_id, hotel_id, check_in and check_out are all required"));
        }

        var entity = new BookingEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = request.UserId,
            HotelId = request.HotelId,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Status = "CONFIRMED",
        };

        _db.Bookings.Add(entity);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Booking created: {BookingId}", entity.Id);

        // Publish a BookingCreated event (best-effort) after the booking is
        // committed. Propagate the correlation id from the incoming gRPC metadata.
        var requestId = context.RequestHeaders.GetValue(Correlation.MetadataKey);
        await _publisher.PublishBookingCreatedAsync(
            entity.Id, entity.UserId, entity.HotelId, requestId, context.CancellationToken);

        return ToProto(entity);
    }

    private static Booking ToProto(BookingEntity e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        HotelId = e.HotelId,
        CheckIn = e.CheckIn,
        CheckOut = e.CheckOut,
        Status = e.Status,
    };
}
