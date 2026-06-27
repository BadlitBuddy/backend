using System.ComponentModel.DataAnnotations.Schema;
using Api.Domain.Interfaces;

namespace Api.Domain.Common;

public abstract class BaseEntity<T> : IBaseEntity
{
    public T Id { get; set; } = default!;
    public string PublicId { get; set; } = Guid.CreateVersion7().ToString();

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public Guid? DeletedById { get; set; }
    public User? DeletedByUser { get; set; }
    public List<BaseEvent> _domainEvents { get; set; } = [];
    [NotMapped] public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}