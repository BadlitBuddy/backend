namespace Api.Domain.Entities;

public class OrganizationSubscription : BaseAuditableEntity<int>
{
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
    
    public DateTimeOffset PlanStart { get; set; }
    public DateTimeOffset PlanEnd { get; set; }
}