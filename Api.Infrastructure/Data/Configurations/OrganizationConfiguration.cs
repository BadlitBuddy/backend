namespace Api.Infrastructure.Data.Configurations;

public class OrganizationConfiguration : BaseAuditableEntityConfiguration<Organization, int>
{
    public override void Configure(EntityTypeBuilder<Organization> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
    }
}
