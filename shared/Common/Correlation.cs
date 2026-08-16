namespace AzMicroApp.Common;

/// <summary>
/// Shared names for the request-correlation id, used both as an HTTP header
/// (at the Gateway edge) and as gRPC metadata (service-to-service).
/// </summary>
public static class Correlation
{
    /// <summary>HTTP header carrying the correlation id at the public edge.</summary>
    public const string HeaderName = "X-Request-ID";

    /// <summary>
    /// gRPC metadata key carrying the correlation id downstream.
    /// gRPC metadata keys must be lower-case.
    /// </summary>
    public const string MetadataKey = "x-request-id";

    /// <summary>Key under which the id is stored in the logging scope / Activity.</summary>
    public const string LogPropertyName = "request_id";
}
