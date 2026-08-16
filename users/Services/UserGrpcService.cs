using AzMicroApp.Protos;
using AzMicroApp.Users.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AzMicroApp.Users.Services;

public sealed class UserGrpcService : UserService.UserServiceBase
{
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(ILogger<UserGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<User> GetUser(UserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("GetUser called for user_id={UserId}", request.UserId);

        if (!UserSeed.Users.TryGetValue(request.UserId, out var user))
        {
            _logger.LogWarning("User not found: {UserId}", request.UserId);
            throw new RpcException(new Status(
                StatusCode.NotFound, $"User '{request.UserId}' not found"));
        }

        return Task.FromResult(user);
    }
}
