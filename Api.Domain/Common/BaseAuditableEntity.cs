using Api.Domain.Interfaces;

namespace Api.Domain.Common;

public abstract class BaseAuditableEntity<T> : BaseEntity<T>, IBaseAuditableEntity
{
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public User? LastModifiedByUser { get; set; }
}