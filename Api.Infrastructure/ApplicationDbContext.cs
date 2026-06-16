using System.Reflection;
using Api.Application.Common.Interfaces;
using Api.Infrastructure.Identity;

namespace Api.Infrastructure;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations =>  Set<Organization>();
    public DbSet<OrganizationSubscription> OrganizationSubscriptions =>  Set<OrganizationSubscription>();
    public DbSet<SubscriptionPlan> SubscriptionPlans =>  Set<SubscriptionPlan>();
    public DbSet<TranscriptionJob> TranscriptionJobs =>  Set<TranscriptionJob>();

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
        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
