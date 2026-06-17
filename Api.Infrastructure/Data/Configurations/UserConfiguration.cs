namespace Api.Infrastructure.Data.Configurations;

public class UserConfiguration
    : BaseAuditableEntityConfiguration<User, Guid>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(200);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}