namespace Api.Application.Common.Interfaces;

public record UserClaimsDto(Guid id, Guid publicId, string? email);

public interface IJwtTokenGenerator
{
    Task<string> CreateAccessTokenAsync(UserClaimsDto userClaimsDto);
}