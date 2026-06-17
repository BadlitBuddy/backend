using System.Diagnostics.CodeAnalysis;

namespace Api.Domain.Entities;

public class User : BaseAuditableEntity<Guid>
{
    private User(){}

    [SetsRequiredMembers]
    public User(Guid userId, Guid publicId, string email, string firstName, string lastName)
    {
        Id = userId;
        PublicId = publicId.ToString();
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
    
    public int? OrganizationId { get; private set; }
    public Organization? Organization { get; private set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; private set; }
    public List<TranscriptionJob> TranscriptionJobs { get; private set; } = [];
}