using System.Text.Json;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;
using StackExchange.Redis;

namespace Shared.Infrastructure.Services;

public class RedisMessagePublisher : IMessagePublisher
{
    private readonly IConnectionMultiplexer _connection;

    public RedisMessagePublisher(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task<long> PublishAsync<T>(MessageChannel channel, T payload)
    {
        ISubscriber publisher = _connection.GetSubscriber();
        string jsonMessage = JsonSerializer.Serialize(payload);

        return await publisher.PublishAsync(RedisChannel.Literal(channel), jsonMessage);
    }
}