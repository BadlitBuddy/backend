namespace Api.Application.Common.Models;

public class BaseEntityDto
{
    public string PublicId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
