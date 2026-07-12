using Api.Application.Common.Interfaces;
using Api.Application.Users.Dtos;

namespace Api.Application.Users.Queries.GetCurrentUserDetails;

public class GetCurrentUserDetailsQuery : IRequest<Result<UserDetailsDto>>
{
}

public class GetCurrentUserDetailsHandler : IRequestHandler<GetCurrentUserDetailsQuery, Result<UserDetailsDto>>
{
    private readonly IUser _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public GetCurrentUserDetailsHandler(IUser currentUser, IApplicationDbContext dbContext)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public async Task<Result<UserDetailsDto>> Handle(GetCurrentUserDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;
        var refreshToken = _currentUser.RefreshToken;
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<UserDetailsDto>.Unauthorized(["Unauthorized Access"]);
        }

        var existingToken = _dbContext.RefreshTokens.SingleOrDefault(t =>
            t.Token == refreshToken && t.UserId == new Guid(userId) && t.IsActive);

        if (existingToken == null)
        {
            return Result<UserDetailsDto>.Unauthorized(["Unauthorized Access"]);
        }

        var existingUser = _dbContext.DomainUsers.SingleOrDefault(t => t.Id == new Guid(userId));
        if (existingUser == null)
        {
            return Result<UserDetailsDto>.Unauthorized(["Unauthorized Access"]);
        }

        var userDto = new UserDetailsDto
        {
            PublicId = existingUser.PublicId,
            CreatedAt = existingUser.Created,
            ModifiedAt = existingUser.LastModified,
            Firstname = existingUser.FirstName,
            LastName = existingUser.LastName,
            Email = existingUser.Email
        };

        return Result<UserDetailsDto>.Success(userDto);
    }
}
