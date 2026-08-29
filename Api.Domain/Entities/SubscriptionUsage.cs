namespace Api.Domain.Entities;

public class SubscriptionUsage : BaseAuditableEntity<int>
{
    public int OrganizationSubscriptionId { get; set; }
    public OrganizationSubscription? OrganizationSubscription { get; set; }

    public long MinutesUsed { get; private set; }

    public long MinutesLimit =>
        OrganizationSubscription?.SubscriptionPlan?.TranscriptionMinutesLimit ?? 0;

    public long MinutesRemaining =>
        Math.Max(0, MinutesLimit - MinutesUsed);

    public void AddUsage(long minutes)
    {
        if (minutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes));
        }

        MinutesUsed += minutes;
    }
}
