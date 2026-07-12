namespace Api.Application.Users.Dtos;

public class UserDetailsDto : BaseEntityDto
{
    public string Firstname { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
}
