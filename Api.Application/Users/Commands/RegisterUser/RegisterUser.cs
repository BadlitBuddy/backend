using Api.Application.Common.Interfaces;
using Api.Domain.Entities;

namespace Api.Application.Users.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<Result<string>>
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string? LastName { get; set; }
    public required bool HasAcceptedTerms { get; set; }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<string>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public RegisterUserCommandHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<Result<string>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _identityService.DoesUserExistByEmailAsync(request.Email))
        {
            return Result<string>.Failure([
                $"Failed to create user with email:  {request.Email}, Please try a different email."
            ]);
        }

        var userGuid = Guid.CreateVersion7();
        var userPublicGuid = Guid.CreateVersion7();

        var domainUser = new User(userGuid, userPublicGuid, request.Email, request.FirstName, request.LastName,
            request.HasAcceptedTerms);
        _dbContext.DomainUsers.Add(domainUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var (result, userId) = await _identityService.CreateUserAsync(userGuid, userPublicGuid, request.FirstName,
            request.LastName, request.Email, request.Password);

        if (!result.Succeeded)
        {
            _dbContext.DomainUsers.Remove(domainUser);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<string>.Failure(result.Errors);
        }

        return Result<string>.Success(userGuid.ToString());
    }
}
