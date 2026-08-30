namespace Api.Domain.Entities;

public class Organization : BaseAuditableEntity<int>
{
    public string Name { get; private set; } = "DEFAULT";
    public OrganizationSubscription? OrganizationSubscription { get; private set; }

    private Organization()
    {
    }

    public Organization(string name)
    {
        Name = name;

        var newOrgSubscription = new OrganizationSubscription(SubscriptionStatus.Active, DateTimeOffset.UtcNow);
        OrganizationSubscription = newOrgSubscription;
    }
}
