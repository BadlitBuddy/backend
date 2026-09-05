namespace Api.Infrastructure.Data.Configurations;

public class OrganizationSubscriptionConfigurations : BaseAuditableEntityConfiguration<OrganizationSubscription, int>
{
    public override void Configure(EntityTypeBuilder<OrganizationSubscription> builder)
    {
        base.Configure(builder);

        builder.HasOne(x => x.Organization)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
