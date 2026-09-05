using Api.Application.Common.Interfaces;
using Api.Domain.Enums;

namespace Api.Application.Users.Queries.GetCurrentUserSubscriptionDetails;

public class SubscriptionDetailsDto
{
    public SubscriptionType SubscriptionType { get; set; }
    public string SubscriptionTypeDesc => SubscriptionType.GetDescription();

    public long TranscriptionMinutesLimit { get; set; }
    public long MinutesUsed { get; set; }
    public long MinutesRemaining => Math.Max(0, TranscriptionMinutesLimit - MinutesUsed);

    public DateTimeOffset PlanStart { get; set; }
    public DateTimeOffset PlanEnd { get; set; }
}

public class GetCurrentUserSubscriptionDetailsQuery : IRequest<Result<SubscriptionDetailsDto>>
{
}

public class GetCurrentUserSubscriptionDetailsHandler : IRequestHandler<GetCurrentUserSubscriptionDetailsQuery,
    Result<SubscriptionDetailsDto>>
{
    private readonly IUser _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public GetCurrentUserSubscriptionDetailsHandler(IUser currentUser, IApplicationDbContext dbContext)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public async Task<Result<SubscriptionDetailsDto>> Handle(GetCurrentUserSubscriptionDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<SubscriptionDetailsDto>.Unauthorized(["Unauthorized Access"]);
        }

        var existingToken =
            await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == new Guid(userId) && t.IsActive,
                cancellationToken: cancellationToken);
        if (existingToken == null)
        {
            return Result<SubscriptionDetailsDto>.Unauthorized(["Unauthorized Access"]);
        }

        var existingUser = await _dbContext.DomainUsers.SingleOrDefaultAsync(t => t.Id == new Guid(userId),
            cancellationToken: cancellationToken);
        if (existingUser == null)
        {
            return Result<SubscriptionDetailsDto>.Unauthorized(["Unauthorized Access"]);
        }

        var userOrganization = await _dbContext.Organizations
            .AsNoTracking()
            .Include(org =>
                org.Subscriptions.Where(subs =>
                    subs.IsActive && !(DateTimeOffset.UtcNow > subs.PlanEnd) && subs.SubscriptionStatus == SubscriptionStatus.Active))
            .ThenInclude(orgSub => orgSub.SubscriptionPlan)
            .SingleOrDefaultAsync(org => org.Id == existingUser.OrganizationId, cancellationToken: cancellationToken);

        if (userOrganization?.CurrentSubscription == null)
        {
            return Result<SubscriptionDetailsDto>.Unauthorized(["Unauthorized Access"]);
        }

        var subscriptionDetails = new SubscriptionDetailsDto()
        {
            SubscriptionType = userOrganization.CurrentSubscription.SubscriptionPlan!.SubscriptionType,
            TranscriptionMinutesLimit = userOrganization.CurrentSubscription.SubscriptionPlan.TranscriptionMinutesLimit,
            MinutesUsed = userOrganization.CurrentSubscription.MinutesUsed,
            PlanEnd = userOrganization.CurrentSubscription.PlanEnd,
            PlanStart = userOrganization.CurrentSubscription.PlanStart
        };

        return Result<SubscriptionDetailsDto>.Success(subscriptionDetails);
    }
}
