namespace Api.Domain.Entities;

public class OrganizationSubscription : BaseAuditableEntity<int>
{
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public SubscriptionStatus SubscriptionStatus { get; private set; }

    public List<SubscriptionUsage> SubscriptionUsages { get; private set; } = [];

    public DateTimeOffset PlanStart { get; private set; }
    public DateTimeOffset PlanEnd { get; private set; }

    private OrganizationSubscription()
    {
    }

    public OrganizationSubscription(SubscriptionStatus subscriptionStatus, DateTimeOffset planStart)
    {
        SubscriptionStatus = subscriptionStatus;
        PlanStart = planStart;
        PlanEnd = planStart.AddDays(30);

        var newSubscriptionPlan =
            new SubscriptionPlan("Default Plan", "Default Plan", 0, "USD", BillingInterval.Monthly, SubscriptionType.Free);
        SubscriptionPlan = newSubscriptionPlan;

        var newSubscriptionUsage = new SubscriptionUsage(0);
        SubscriptionUsages.Add(newSubscriptionUsage);
    }
}
