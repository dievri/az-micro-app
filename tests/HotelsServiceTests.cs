using AzMicroApp.Protos;
using AzMicroApp.Hotels.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AzMicroApp.Tests;

public class HotelsServiceTests
{
    private static HotelGrpcService NewService() =>
        new(NullLogger<HotelGrpcService>.Instance);

    [Fact]
    public async Task GetHotel_ReturnsSeededHotel()
    {
        var svc = NewService();
        var hotel = await svc.GetHotel(new HotelRequest { HotelId = "h1" }, TestServerCallContext.Create());

        Assert.Equal("h1", hotel.Id);
        Assert.Equal("Grand Riverside", hotel.Name);
        Assert.Equal("Kyiv", hotel.City);
    }

    [Fact]
    public async Task GetHotel_UnknownId_ThrowsNotFound()
    {
        var svc = NewService();
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            svc.GetHotel(new HotelRequest { HotelId = "nope" }, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
