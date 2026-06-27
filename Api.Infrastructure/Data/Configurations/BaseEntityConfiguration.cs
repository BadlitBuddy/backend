using Api.Domain.Common;

namespace Api.Infrastructure.Data.Configurations;

public abstract class BaseEntityConfiguration<TEntity, TKey>
    : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity<TKey>
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);


        builder.Property(e => e.PublicId)
            .HasMaxLength(50);

        builder.Ignore(e => e.DomainEvents);
        builder.Ignore("_domainEvents");

        builder.Property(e => e.DeletedById);
        builder.Property(e => e.DeletedBy)
            .HasMaxLength(50);

        builder.HasOne(e => e.DeletedByUser)
            .WithMany()
            .HasForeignKey(e => e.DeletedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}