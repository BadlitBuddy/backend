namespace Api.Domain.Entities;

public class Organization : BaseAuditableEntity<int>
{
    public required string Name { get; set; }
}