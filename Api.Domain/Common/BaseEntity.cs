namespace Api.Domain.Common;

public abstract class BaseEntity<T>
{
    public T Id { get; set; } = default!;
    public string PublicId { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public int? DeletedById { get; set; }
    public User? DeletedByUser { get; set; }
}