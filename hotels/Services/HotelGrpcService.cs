using AzMicroApp.Protos;
using AzMicroApp.Hotels.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Hotels.Services;

public sealed class HotelGrpcService : HotelService.HotelServiceBase
{
    private readonly ILogger<HotelGrpcService> _logger;

    public HotelGrpcService(ILogger<HotelGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<Hotel> GetHotel(HotelRequest request, ServerCallContext context)
    {
        _logger.LogInformation("GetHotel called for hotel_id={HotelId}", request.HotelId);

        if (!HotelSeed.Hotels.TryGetValue(request.HotelId, out var hotel))
        {
            _logger.LogWarning("Hotel not found: {HotelId}", request.HotelId);
            throw new RpcException(new Status(
                StatusCode.NotFound, $"Hotel '{request.HotelId}' not found"));
        }

        return Task.FromResult(hotel);
    }
}
