using Shared.Contracts.Enums;

namespace Shared.Abstractions.Services;

public interface IMessageSubscriber
{
    IAsyncEnumerable<T> SubscribeAsync<T>(MessageChannel channel);
}