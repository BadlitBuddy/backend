namespace Api.Domain.Interfaces;

public interface IBaseAuditableEntity : IBaseEntity
{
    DateTimeOffset Created { get; set; }
    string? CreatedBy { get; set; }
    Guid? CreatedByUserId { get; set; }
    User? CreatedByUser { get; set; }

    DateTimeOffset? LastModified { get; set; }
    string? LastModifiedBy { get; set; }
    Guid? LastModifiedByUserId { get; set; }
    User? LastModifiedByUser { get; set; }
}