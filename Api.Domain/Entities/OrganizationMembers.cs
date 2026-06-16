namespace Api.Domain.Entities;

public class OrganizationMembers : BaseEntity<int>
{
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }
}