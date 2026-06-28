using System.Text.Json;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;
using StackExchange.Redis;

namespace Shared.Infrastructure.Services;

public class RedisMessageSubscriber : IMessageSubscriber
{
    private readonly IConnectionMultiplexer _connection;

    public RedisMessageSubscriber(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async IAsyncEnumerable<T> SubscribeAsync<T>(MessageChannel channel)
    {
        ISubscriber subscriber = _connection.GetSubscriber();

        ChannelMessageQueue queue = await subscriber.SubscribeAsync(RedisChannel.Literal(channel));

        await foreach (ChannelMessage channelMessage in queue)
        {
            if (channelMessage.Message.IsNullOrEmpty)
                continue;

            T? payload = JsonSerializer.Deserialize<T>(
                channelMessage.Message.ToString());

            if (payload is not null)
                yield return payload;
        }
    }
}