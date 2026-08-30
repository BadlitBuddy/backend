using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class OrganizationSubscription : BaseAuditableEntity<int>
{
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public SubscriptionStatus SubscriptionStatus { get; private set; }
    public DateTimeOffset PlanStart { get; private set; }
    public DateTimeOffset PlanEnd { get; private set; }
    [NotMapped] public bool IsExpired => DateTimeOffset.UtcNow > PlanEnd;

    public long MinutesUsed { get; private set; }
    [NotMapped] public long MinutesLimit => SubscriptionPlan?.TranscriptionMinutesLimit ?? 0;
    [NotMapped] public long MinutesRemaining => Math.Max(0, MinutesLimit - MinutesUsed);

    private OrganizationSubscription()
    {
    }

    public OrganizationSubscription(SubscriptionStatus subscriptionStatus, DateTimeOffset planStart)
    {
        SubscriptionStatus = subscriptionStatus;
        PlanStart = planStart;
        PlanEnd = planStart.AddDays(30);
    }

    public void SetSubscriptionPlan(SubscriptionPlan subscriptionPlan)
    {
        SubscriptionPlan = subscriptionPlan;
    }
}
