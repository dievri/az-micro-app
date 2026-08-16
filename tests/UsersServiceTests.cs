using AzMicroApp.Protos;
using AzMicroApp.Users.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AzMicroApp.Tests;

public class UsersServiceTests
{
    private static UserGrpcService NewService() =>
        new(NullLogger<UserGrpcService>.Instance);

    [Fact]
    public async Task GetUser_ReturnsSeededUser()
    {
        var svc = NewService();
        var user = await svc.GetUser(new UserRequest { UserId = "u1" }, TestServerCallContext.Create());

        Assert.Equal("u1", user.Id);
        Assert.Equal("Alice Johnson", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public async Task GetUser_UnknownId_ThrowsNotFound()
    {
        var svc = NewService();
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            svc.GetUser(new UserRequest { UserId = "nope" }, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
