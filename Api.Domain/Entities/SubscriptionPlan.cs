namespace Api.Domain.Entities;

public class SubscriptionPlan : BaseAuditableEntity<int>
{
    public required string Name  { get; set; }
    public required string Description  { get; set; }
    
    public int Price { get; set; }
    public required string Currency { get; set; }
    
    public BillingInterval BillingInterval { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
}