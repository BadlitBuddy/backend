using Api.Domain.Common;

namespace Api.Infrastructure.Data.Configurations;

public abstract class BaseAuditableEntityConfiguration<TEntity, TKey>
    : BaseEntityConfiguration<TEntity, TKey>
    where TEntity : BaseAuditableEntity<TKey>
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);
        
        builder.Property(e => e.CreatedByUserId)
            .HasMaxLength(100);
        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50);

        builder.Property(e => e.LastModifiedByUserId)
            .HasMaxLength(100);
        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(50);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
