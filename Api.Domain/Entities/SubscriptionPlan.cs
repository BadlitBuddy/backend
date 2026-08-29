namespace Api.Domain.Entities;

public class SubscriptionPlan : BaseAuditableEntity<int>
{
    public required string Name { get; set; }
    public required string Description { get; set; }

    public decimal Price { get; set; }
    public required string Currency { get; set; }

    public BillingInterval BillingInterval { get; set; }
    public SubscriptionType SubscriptionType { get; set; }

    public long TranscriptionMinutesLimit { get; private set; }

    private SubscriptionPlan()
    {
    }

    public SubscriptionPlan(string name, string description, decimal price, BillingInterval billingInterval,
        SubscriptionType subscriptionType)
    {
        Name = name;
        Description = description;
        Price = price;
        BillingInterval = billingInterval;
        SetSubscriptionType(subscriptionType);
    }

    public void SetSubscriptionType(SubscriptionType subscriptionType)
    {
        switch (subscriptionType)
        {
            case SubscriptionType.Free:
                SubscriptionType = SubscriptionType.Free;
                TranscriptionMinutesLimit = 300;
                break;
            case SubscriptionType.Starter:
                SubscriptionType = SubscriptionType.Starter;
                TranscriptionMinutesLimit = 1800;
                break;
            case SubscriptionType.Pro:
                SubscriptionType = SubscriptionType.Pro;
                TranscriptionMinutesLimit = 6000;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(subscriptionType), subscriptionType, null);
        }
    }
}
