using Dapper;
using Shared.Abstractions.Repositories;
using Shared.Infrastructure.Data;

namespace Shared.Infrastructure.Repositories;

public class OrganizationSubscriptionRepository : IOrganizationSubscriptionRepository
{
    private readonly DapperDbContext _dbContext;

    public OrganizationSubscriptionRepository(DapperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> UpdateMinutesUsedByIdAsync(int id, long minutesUsed)
    {
        using var connection = _dbContext.CreateConnection();

        const string command = """
                               UPDATE public."OrganizationSubscriptions" SET "MinutesUsed" = @MinutesUsed WHERE "Id" = @Id;
                               """;
        var rowsAffected = await connection.ExecuteAsync(
            command,
            new
            {
                Id = id,
                MinutesUsed = minutesUsed
            });

        return rowsAffected > 0;
    }
}
