namespace Api.Application.Common.Interfaces;

public record GeneratedTokenDto(string AccessToken, string RefreshToken);

public interface ITokenService
{
    Task<GeneratedTokenDto> CreateTokensAsync(string userId);
    Task<GeneratedTokenDto> RefreshTokensAsync(string refreshToken);
    Task RevokeAsync(string refreshToken, Guid userId);
}
