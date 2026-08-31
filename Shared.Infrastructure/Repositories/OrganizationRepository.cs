using Api.Domain.Entities;
using Dapper;
using Shared.Abstractions.Repositories;
using Shared.Infrastructure.Data;

namespace Shared.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly DapperDbContext _dbContext;

    public OrganizationRepository(DapperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Organization?> GetWithSubscriptionDetailsByIdAsync(
        int organizationId)
    {
        using var connection = _dbContext.CreateConnection();

        const string query = """
                             SELECT *
                             FROM public."Organizations"
                             WHERE "Id" = @OrganizationId;

                             SELECT *
                             FROM public."OrganizationSubscriptions"
                             WHERE "OrganizationId" = @OrganizationId
                               AND "SubscriptionStatus" = 0
                               AND "IsActive" = true
                             ORDER BY "Created" DESC;

                             SELECT sp.*
                             FROM public."SubscriptionPlans" sp
                             INNER JOIN public."OrganizationSubscriptions" os
                                 ON sp."Id" = os."SubscriptionPlanId"
                             WHERE os."OrganizationId" = @OrganizationId
                               AND os."SubscriptionStatus" = 0
                               AND os."IsActive" = true;
                             """;

        await using var multi = await connection.QueryMultipleAsync(
            query,
            new { OrganizationId = organizationId });

        var organization =
            await multi.ReadSingleOrDefaultAsync<Organization>();

        if (organization is null)
            return null;

        var subscriptions =
            (await multi.ReadAsync<OrganizationSubscription>()).ToList();

        var subscriptionPlans =
            (await multi.ReadAsync<SubscriptionPlan>())
            .ToDictionary(x => x.Id);

        foreach (var subscription in subscriptions)
        {
            if (subscriptionPlans.TryGetValue(
                    subscription.SubscriptionPlanId,
                    out var plan))
            {
                subscription.SubscriptionPlan = plan;
            }
        }

        organization.Subscriptions.AddRange(subscriptions);

        return organization;
    }
}
