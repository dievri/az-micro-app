using Grpc.Core;

namespace AzMicroApp.Tests;

/// <summary>
/// Minimal ServerCallContext for unit-testing gRPC service classes directly,
/// without hosting a real server.
/// </summary>
public sealed class TestServerCallContext : ServerCallContext
{
    private readonly Metadata _requestHeaders;

    private TestServerCallContext(Metadata requestHeaders) => _requestHeaders = requestHeaders;

    public static TestServerCallContext Create(Metadata? requestHeaders = null) =>
        new(requestHeaders ?? new Metadata());

    protected override string MethodCore => "TestMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "localhost";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => _requestHeaders;
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore { get; } = new();
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new("", new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
