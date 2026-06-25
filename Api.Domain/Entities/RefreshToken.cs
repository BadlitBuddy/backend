namespace Api.Domain.Entities;

public class RefreshToken : BaseAuditableEntity<Guid>
{
    public required string Token { get; set; }
    public string? ReplacedByToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
}