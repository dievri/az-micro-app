using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

// ---------------------------------------------------------------------------
// Notifications consumer.
//
// Runs as an event-driven Azure Container Apps Job: KEDA's Service Bus scaler
// starts a job execution when messages arrive on the queue. Each execution
// drains the currently available messages, "processes" them (here: logs a
// simulated notification), completes them (peek-lock -> Complete), and exits
// so the job can scale back to zero.
//
// Authentication is passwordless via Managed Identity (DefaultAzureCredential).
// The job's identity needs the "Azure Service Bus Data Receiver" role.
// ---------------------------------------------------------------------------

const string ServiceName = "notifications";

using var loggerFactory = LoggerFactory.Create(b =>
    b.AddJsonConsole(o =>
    {
        o.UseUtcTimestamp = true;
        o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    }));
var logger = loggerFactory.CreateLogger(ServiceName);

var sbNamespace = Environment.GetEnvironmentVariable("SERVICEBUS_NAMESPACE");
var topicName = Environment.GetEnvironmentVariable("SERVICEBUS_TOPIC");
var subscription = Environment.GetEnvironmentVariable("SERVICEBUS_SUBSCRIPTION");
var queueName = Environment.GetEnvironmentVariable("SERVICEBUS_QUEUE") ?? "bookings";
var consumerRole = Environment.GetEnvironmentVariable("CONSUMER_ROLE") ?? "notifications";

if (string.IsNullOrWhiteSpace(sbNamespace))
{
    logger.LogError("SERVICEBUS_NAMESPACE is not set. Nothing to consume; exiting.");
    return 1;
}

await using var client = new ServiceBusClient(sbNamespace, new DefaultAzureCredential());

// Read from a Topic subscription (fan-out) when both SERVICEBUS_TOPIC and
// SERVICEBUS_SUBSCRIPTION are set; otherwise read from a Queue. The receiver
// API and processing loop below are identical for both.
var options = new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock };
ServiceBusReceiver receiver;
string source;
if (!string.IsNullOrWhiteSpace(topicName) && !string.IsNullOrWhiteSpace(subscription))
{
    receiver = client.CreateReceiver(topicName, subscription, options);
    source = $"topic '{topicName}' subscription '{subscription}'";
}
else
{
    receiver = client.CreateReceiver(queueName, options);
    source = $"queue '{queueName}'";
}
await using var _receiver = receiver;

logger.LogInformation("{Service} started, draining {Source} on {Namespace}",
    ServiceName, source, sbNamespace);

var processed = 0;

// Drain the messages currently available. When no more arrive within the short
// wait window, we stop and let the job execution finish.
while (true)
{
    var batch = await receiver.ReceiveMessagesAsync(
        maxMessages: 10,
        maxWaitTime: TimeSpan.FromSeconds(5));

    if (batch.Count == 0)
    {
        break; // queue drained -> exit so the job scales back to zero
    }

    foreach (var message in batch)
    {
        try
        {
            // The event payload published by the bookings service. The publisher
            // serializes with camelCase property names (bookingId, userId, ...),
            // so deserialize case-insensitively to map them onto the record.
            var evt = JsonSerializer.Deserialize<BookingCreatedEvent>(
                message.Body.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Correlation id flows from the original HTTP request through gRPC
            // metadata, into the Service Bus message, and now into our logs.
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["request_id"] = message.CorrelationId ?? "(none)"
            }))
            {
                // The same image can act as different fan-out consumers depending
                // on CONSUMER_ROLE (e.g. "notifications" vs "analytics"), each
                // reacting to the SAME BookingCreated event in its own way.
                if (consumerRole == "analytics")
                {
                    logger.LogInformation(
                        "Recorded booking analytics: booking {BookingId}, user {UserId}, hotel {HotelId}",
                        evt?.BookingId, evt?.UserId, evt?.HotelId);
                }
                else
                {
                    logger.LogInformation(
                        "Sent booking-confirmation notification for booking {BookingId} to user {UserId} (hotel {HotelId})",
                        evt?.BookingId, evt?.UserId, evt?.HotelId);
                }
            }

            // Acknowledge successful processing: removes the message from the queue.
            await receiver.CompleteMessageAsync(message);
            processed++;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message {MessageId}; abandoning", message.MessageId);
            // Abandon -> lock released, message becomes visible again for retry.
            // After MaxDeliveryCount attempts Service Bus moves it to the DLQ.
            await receiver.AbandonMessageAsync(message);
        }
    }
}

logger.LogInformation("{Service} finished; processed {Count} message(s)", ServiceName, processed);
return 0;

internal sealed record BookingCreatedEvent(
    string? EventType, string? BookingId, string? UserId, string? HotelId);
