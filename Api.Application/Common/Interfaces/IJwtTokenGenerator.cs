namespace Api.Application.Common.Interfaces;

public record UserClaimsDto(Guid Id, Guid PublicId, string? Email);

public interface IJwtTokenGenerator
{
    Task<string> CreateAccessTokenAsync(UserClaimsDto userClaimsDto);
}