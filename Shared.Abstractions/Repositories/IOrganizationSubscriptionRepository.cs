namespace Shared.Abstractions.Repositories;

public interface IOrganizationSubscriptionRepository
{
    Task<bool> UpdateMinutesUsedByIdAsync(int id, long minutesUsed);
}
