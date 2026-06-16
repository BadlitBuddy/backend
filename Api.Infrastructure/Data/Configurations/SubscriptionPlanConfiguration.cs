namespace Api.Infrastructure.Data.Configurations;

public class SubscriptionPlanConfiguration : BaseAuditableEntityConfiguration<SubscriptionPlan, int>
{
    public override void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        base.Configure(builder);
        
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(500).IsRequired();
    }
}