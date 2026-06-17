namespace Api.Domain.Interfaces;

public interface IBaseAuditableEntity : IBaseEntity
{
    DateTimeOffset Created { get; set; }
    string? CreatedBy { get; set; }
    string? CreatedByUserId { get; set; }
    User? CreatedByUser { get; set; }

    DateTimeOffset? LastModified { get; set; }
    string? LastModifiedBy { get; set; }
    string? LastModifiedByUserId { get; set; }
    User? LastModifiedByUser { get; set; }
}