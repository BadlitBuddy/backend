namespace Api.Infrastructure.Data.Configurations;

public class SubscriptionUsageConfiguration : BaseAuditableEntityConfiguration<SubscriptionUsage, int>
{
    public override void Configure(EntityTypeBuilder<SubscriptionUsage> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => new { x.OrganizationSubscriptionId, x.PublicId });

        builder.HasOne(x => x.OrganizationSubscription)
            .WithMany(x => x.SubscriptionUsages)
            .HasForeignKey(x => x.OrganizationSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
