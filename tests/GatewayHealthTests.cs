extern alias gateway;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using GatewayProgram = gateway::Program;

namespace AzMicroApp.Tests;

/// <summary>
/// Boots the real Gateway (its Program) with an in-memory server and verifies
/// the public /health endpoint. The gRPC clients are constructed lazily, so
/// /health works without the internal services being reachable.
/// </summary>
public class GatewayHealthTests : IClassFixture<WebApplicationFactory<GatewayProgram>>
{
    private readonly WebApplicationFactory<GatewayProgram> _factory;

    public GatewayHealthTests(WebApplicationFactory<GatewayProgram> factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
        Assert.Contains("\"service\":\"gateway\"", body);
    }

    [Fact]
    public async Task Health_EchoesRequestIdHeader()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Request-ID", "test-correlation-123");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Request-ID", out var values));
        Assert.Contains("test-correlation-123", values!);
    }
}
