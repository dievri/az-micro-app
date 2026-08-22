using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Bookings.Messaging;

/// <summary>
/// Publishes a "BookingCreated" event to Azure Service Bus after a booking is
/// persisted. Authentication is passwordless (Managed Identity) — see Program.cs
/// where the ServiceBusClient is registered with DefaultAzureCredential.
///
/// Publishing is best-effort: if Service Bus isn't configured (local dev) the
/// publisher is a no-op, and a publish failure is logged but never fails the
/// booking itself (the booking is already committed to the database).
/// </summary>
public sealed class BookingEventPublisher
{
    private readonly ServiceBusSender? _sender;
    private readonly ILogger<BookingEventPublisher> _logger;

    public BookingEventPublisher(ServiceBusClient? client, ILogger<BookingEventPublisher> logger)
    {
        _logger = logger;

        // Send to a Topic if SERVICEBUS_TOPIC is set (fan-out to multiple
        // subscriptions), otherwise fall back to a Queue. The sender API is
        // identical for both — only the target entity name differs.
        var target = Environment.GetEnvironmentVariable("SERVICEBUS_TOPIC")
                     ?? Environment.GetEnvironmentVariable("SERVICEBUS_QUEUE")
                     ?? "bookings";
        _sender = client?.CreateSender(target);
    }

    public async Task PublishBookingCreatedAsync(
        string bookingId, string userId, string hotelId, string? requestId, CancellationToken ct)
    {
        if (_sender is null)
        {
            _logger.LogDebug("Service Bus not configured; skipping BookingCreated publish for {BookingId}", bookingId);
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                eventType = "BookingCreated",
                bookingId,
                userId,
                hotelId,
            });

            var message = new ServiceBusMessage(payload)
            {
                ContentType = "application/json",
                Subject = "BookingCreated",
                MessageId = bookingId, // enables dedup if it were turned on
            };

            // Carry the correlation id so the consumer can log against the same request.
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                message.CorrelationId = requestId;
            }

            await _sender.SendMessageAsync(message, ct);
            _logger.LogInformation("Published BookingCreated event for {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            // Never fail the booking because the event couldn't be published.
            _logger.LogError(ex, "Failed to publish BookingCreated event for {BookingId}", bookingId);
        }
    }
}
