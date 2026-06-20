using Api.Application.Common.Interfaces;
using Api.Domain.Entities;
using Ardalis.GuardClauses;

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
            throw new NotFoundException("Email","User");
        }
        
        var userGuid = Guid.NewGuid();
        var userPublicGuid  = Guid.NewGuid();
        
        var (result, userId) = await _identityService.CreateUserAsync(userGuid, userPublicGuid, request.FirstName, request.LastName, request.Email, request.Password);

        if (!result.Succeeded)
        {
            throw new ValidationException("Could not create user, please try again.");
        }
            
        var domainUser = new User(userGuid, userPublicGuid, request.Email, request.FirstName, request.LastName);
        _dbContext.Users.Add(domainUser);

        await _dbContext.SaveChangesAsync(cancellationToken);
            
        return domainUser.Id.ToString();
    }
}