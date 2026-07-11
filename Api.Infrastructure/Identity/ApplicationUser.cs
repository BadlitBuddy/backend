using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public required Guid PublicId { get; set; }
    [MaxLength(250)] public required string FirstName { get; set; }
    [MaxLength(250)] public string? LastName { get; set; }
    public bool HasAcceptedTerms { get; set; }
}
