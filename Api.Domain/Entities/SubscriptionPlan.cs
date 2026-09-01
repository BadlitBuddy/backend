namespace Api.Domain.Entities;

public class SubscriptionPlan : BaseAuditableEntity<int>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public BillingInterval BillingInterval { get; private set; }
    public SubscriptionType SubscriptionType { get; private set; }

    public long TranscriptionMinutesLimit { get; private set; }

    private SubscriptionPlan()
    {
    }

    public SubscriptionPlan(string name, string description, decimal price, string currency, BillingInterval billingInterval,
        SubscriptionType subscriptionType)
    {
        Name = name;
        Description = description;
        Price = price;
        Currency = currency;
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
