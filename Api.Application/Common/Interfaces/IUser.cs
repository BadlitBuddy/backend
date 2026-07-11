namespace Api.Application.Common.Interfaces;

public interface IUser
{
    string? RefreshToken { get; }
    string? Id { get; }
    string? Email { get; }
    List<string>? Roles { get; }
}
