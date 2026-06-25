using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> DomainUsers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationSubscription> OrganizationSubscriptions { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<TranscriptionJob> TranscriptionJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}