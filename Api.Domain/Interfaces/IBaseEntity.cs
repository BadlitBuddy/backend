using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Interfaces;

public interface IBaseEntity
{
    string PublicId { get; set; }
    bool IsActive { get; set; }

    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
    Guid? DeletedById { get; set; }
    User? DeletedByUser { get; set; }

    List<BaseEvent> _domainEvents { get; set; }
    [NotMapped] IReadOnlyCollection<BaseEvent> DomainEvents { get; }
    void AddDomainEvent(BaseEvent domainEvent);
    void RemoveDomainEvent(BaseEvent domainEvent);
    void ClearDomainEvents();
}
