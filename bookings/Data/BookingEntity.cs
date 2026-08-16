namespace AzMicroApp.Bookings.Data;

/// <summary>
/// Persistence model for a booking. The Bookings service is the sole owner of
/// this data — no other service touches the database.
/// </summary>
public sealed class BookingEntity
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string HotelId { get; set; } = default!;

    // Stored as plain ISO date strings to keep the lab simple and the gRPC
    // contract (which uses strings) a direct mapping.
    public string CheckIn { get; set; } = default!;
    public string CheckOut { get; set; } = default!;

    public string Status { get; set; } = "CONFIRMED";
}
