namespace AzMicroApp.Gateway.Models;

// Request body for POST /booking
public sealed record CreateBookingRequestDto(
    string UserId,
    string HotelId,
    string CheckIn,
    string CheckOut);

// Aggregated response for GET /booking/{id}
public sealed record BookingDto(
    string Id, string UserId, string HotelId,
    string CheckIn, string CheckOut, string Status);

public sealed record UserDto(string Id, string Name, string Email);

public sealed record HotelDto(
    string Id, string Name, string City, string Country);

public sealed record AggregatedBookingDto(
    BookingDto Booking, UserDto User, HotelDto Hotel);
