using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class Organization : BaseAuditableEntity<int>
{
    public string Name { get; private set; } = "DEFAULT";
    public List<OrganizationSubscription> Subscriptions { get; private set; } = [];

    [NotMapped]
    public OrganizationSubscription? CurrentSubscription =>
        Subscriptions.SingleOrDefault(s => s.SubscriptionStatus == SubscriptionStatus.Active);

    private Organization()
    {
    }

    public Organization(string name)
    {
        Name = name;

        if (Subscriptions.Any(s => s.SubscriptionStatus == SubscriptionStatus.Active))
            throw new InvalidOperationException("A subscription is already active.");

        var newSubscriptionPlan =
            new SubscriptionPlan("Default Plan", "Default Plan", 0, "USD", BillingInterval.Monthly, SubscriptionType.Free);

        var newOrgSubscription = new OrganizationSubscription(SubscriptionStatus.Active, DateTimeOffset.UtcNow);
        newOrgSubscription.SetSubscriptionPlan(newSubscriptionPlan);

        Subscriptions.Add(newOrgSubscription);
    }
}
