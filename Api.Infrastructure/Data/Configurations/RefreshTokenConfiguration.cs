namespace Api.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : BaseAuditableEntityConfiguration<RefreshToken, Guid>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.Token).IsRequired().HasMaxLength(250);
        builder.Property(r => r.ReplacedByToken).HasMaxLength(250);

        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
    }
}