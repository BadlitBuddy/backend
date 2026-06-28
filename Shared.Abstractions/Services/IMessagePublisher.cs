using Shared.Contracts.Enums;

namespace Shared.Abstractions.Services;

public interface IMessagePublisher
{
    Task<long> PublishAsync<T>(MessageChannel channel, T payload);
}