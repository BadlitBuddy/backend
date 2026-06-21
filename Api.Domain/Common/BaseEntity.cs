using Api.Domain.Interfaces;

namespace Api.Domain.Common;

public abstract class BaseEntity<T> : IBaseEntity {
    public T Id { get; set; } = default!;
    public string PublicId { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public Guid? DeletedById { get; set; }
    public User? DeletedByUser { get; set; }
}