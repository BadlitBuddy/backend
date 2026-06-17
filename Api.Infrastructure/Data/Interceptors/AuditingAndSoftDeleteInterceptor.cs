using Api.Application.Common.Interfaces;
using Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api.Infrastructure.Data.Interceptors;

public class AuditingAndSoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IUser _currentUserService;

    public AuditingAndSoftDeleteInterceptor(IUser currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserService.Id;
        var userName = _currentUserService.Email;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IBaseAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.Created = now;
                    auditableEntity.CreatedBy = userName;
                    auditableEntity.CreatedByUserId = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.LastModified = now;
                    auditableEntity.LastModifiedBy = userName;
                    auditableEntity.LastModifiedByUserId = userId;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    
                    auditableEntity.IsActive = false;
                    auditableEntity.DeletedAt = now;
                    auditableEntity.DeletedBy = userName;
                    auditableEntity.DeletedById = userId;
                    
                    auditableEntity.LastModified = now;
                    auditableEntity.LastModifiedBy = userName;
                    auditableEntity.LastModifiedByUserId = userId;
                }
            }
            else if (entry.Entity is IBaseEntity baseEntity)
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    
                    baseEntity.IsActive = false;
                    baseEntity.DeletedAt = now;
                    baseEntity.DeletedBy = userName;
                    baseEntity.DeletedById = userId;
                }
            }
        }
    }
}
