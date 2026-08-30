using System.Reflection;
using Api.Application.Common.Interfaces;
using Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Api.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationSubscription> OrganizationSubscriptions => Set<OrganizationSubscription>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>().Property(u => u.Id).ValueGeneratedNever();
        // 1-1 shared PK: AspNetUsers.Id == Users.Id
        modelBuilder.Entity<ApplicationUser>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<ApplicationUser>(a => a.Id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(u => u.IsActive);
        modelBuilder.Entity<Organization>().HasQueryFilter(u => u.IsActive);
        modelBuilder.Entity<OrganizationSubscription>().HasQueryFilter(u => u.IsActive);
        modelBuilder.Entity<SubscriptionPlan>().HasQueryFilter(u => u.IsActive);
        modelBuilder.Entity<Transcript>().HasQueryFilter(u => u.IsActive);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
