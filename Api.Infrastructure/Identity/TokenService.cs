using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Api.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IApplicationDbContext _dbContext;

    // TODO: put this in configurationS
    private const int RefreshDays = 7;

    public TokenService(UserManager<ApplicationUser> userManager, IConfiguration config,
        IApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _config = config;
        _dbContext = dbContext;
    }

    public async Task<GeneratedTokenDto> CreateTokensAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }

        var accessToken = await CreateAccessTokenAsync(user);
        var refreshToken = await IssueRefreshTokenAsync(user);

        return new GeneratedTokenDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token
        );
    }

    public async Task<GeneratedTokenDto> RefreshTokenAsync(string refreshToken, Guid userId)
    {
        var existingRefreshToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.Token == refreshToken && t.UserId == userId);

        if (existingRefreshToken == null)
        {
            throw new UnauthorizedAccessException();
        }

        if (existingRefreshToken.IsExpired || !existingRefreshToken.IsActive)
        {
            throw new UnauthorizedAccessException();
        }

        var appUser = await _userManager.FindByIdAsync(existingRefreshToken.UserId.ToString());
        if (appUser == null)
        {
            throw new UnauthorizedAccessException();
        }

        var newRefreshToken = await IssueRefreshTokenAsync(appUser);
        var accessToken = await CreateAccessTokenAsync(appUser);

        existingRefreshToken.RevokedAt = DateTimeOffset.UtcNow;
        existingRefreshToken.ReplacedByToken = newRefreshToken.Token;
        existingRefreshToken.IsActive = false;
        await _dbContext.SaveChangesAsync();

        return new GeneratedTokenDto(accessToken, newRefreshToken.Token);
    }

    public async Task RevokeAsync(string refreshToken, Guid userId)
    {
        var token = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.Token == refreshToken && t.UserId == userId);
        if (token == null)
        {
            throw new UnauthorizedAccessException();
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        token.ReplacedByToken = null;
        token.IsActive = false;

        await _dbContext.SaveChangesAsync();
    }

    private async Task<string> CreateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new("public_id", user.PublicId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        claims.AddRange(
            roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _config["Jwt:Key"]!));

        var creds =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private async Task<RefreshToken> IssueRefreshTokenAsync(ApplicationUser user)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var entry = new RefreshToken
        {
            PublicId = Guid.CreateVersion7().ToString(),
            Token = raw,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshDays)
        };

        _dbContext.RefreshTokens.Add(entry);
        await _dbContext.SaveChangesAsync();
        return entry;
    }
}