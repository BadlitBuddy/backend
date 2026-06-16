namespace Api.Domain.Enums;

public enum SubscriptionStatus
{
    [Description("Active")]
    Active,
    [Description("Cancelled")]
    Cancelled,
    [Description("Expired")]
    Expired,
}