using Api.Application.Common.Interfaces;
using Api.Domain.Entities;

namespace Api.Application.Users.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<string>
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string? LastName { get; set; }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, string>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public RegisterUserCommandHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _identityService.DoesUserExistByEmailAsync(request.Email))
        {
            throw new NotFoundException("Email", "User");
        }

        var userGuid = Guid.CreateVersion7();
        var userPublicGuid = Guid.CreateVersion7();

        var domainUser = new User(userGuid, userPublicGuid, request.Email, request.FirstName, request.LastName);
        _dbContext.Users.Add(domainUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var (result, userId) = await _identityService.CreateUserAsync(userGuid, userPublicGuid, request.FirstName,
            request.LastName, request.Email, request.Password);

        if (!result.Succeeded)
        {
            _dbContext.Users.Remove(domainUser);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("An error occurred while registering the user");
        }

        return domainUser.Id.ToString();
    }
}