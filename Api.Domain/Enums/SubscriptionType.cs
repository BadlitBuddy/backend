namespace Api.Domain.Enums;

public enum SubscriptionType
{
    [Description("Free")]
    Free,
    [Description("Monthly")]
    Starter,
    [Description("Pro")]
    Pro,
    [Description("Pro +")]
    ProPlus
}