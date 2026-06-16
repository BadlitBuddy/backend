namespace Api.Domain.Common;

public abstract class BaseAuditableEntity<T> : BaseEntity<T>
{
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public int? LastModifiedByUserId { get; set; }
    public User? LastModifiedByUser { get; set; }
}