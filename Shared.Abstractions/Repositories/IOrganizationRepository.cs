using Api.Domain.Entities;

namespace Shared.Abstractions.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetWithSubscriptionDetailsByIdAsync(int organizationId);
}
