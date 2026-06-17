namespace Api.Domain.Interfaces;

public interface IBaseEntity
{
    string PublicId { get; set; }
    bool IsActive { get; set; }

    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
    string? DeletedById { get; set; }
    User? DeletedByUser { get; set; }
}