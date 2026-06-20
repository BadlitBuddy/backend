using Api.Application.Common.Models;

namespace Api.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);
    Task<bool> DoesUserExistByEmailAsync(string email);
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(Guid userId, Guid publicId, string firstName, string? lastName,
        string email,
        string password);

    Task<Result> DeleteUserAsync(string userId);
}