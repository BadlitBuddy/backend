namespace Api.Infrastructure.Data.Configurations;

public class TranscriptConfiguration : BaseAuditableEntityConfiguration<Transcript, int>
{
    public override void Configure(EntityTypeBuilder<Transcript> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.UnprocessedObjectKey).IsRequired().HasMaxLength(250);
        builder.Property(x => x.ProcessedObjectKey).HasMaxLength(250);

        builder.HasOne(x => x.User)
            .WithMany(x => x.TranscriptionJobs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
