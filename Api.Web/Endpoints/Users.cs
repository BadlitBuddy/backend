using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.RegisterUser;
using Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Web.Endpoints;

public record RegisterUserResponse(string UserId);

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(string AccessToken, string RefreshToken);

public record RefreshTokenRequestDto(string RefreshToken);

public record LogoutRequestDto(string RefreshToken);

public record LogoutResponseDto(string Message);

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("register", Register);
        groupBuilder.MapPost("login", Login);
        groupBuilder.MapPost("refresh", Refresh).RequireAuthorization();
        groupBuilder.MapPost("logout", Logout).RequireAuthorization();
    }

    [EndpointSummary("Register")]
    [EndpointDescription("Registers a new user")]
    public static async Task<Results<ProblemHttpResult, Ok<RegisterUserResponse>>> Register(
        ISender sender, RegisterUserCommand command
    )
    {
        var result = await sender.Send(command);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(new RegisterUserResponse(result.Value!));
    }

    [EndpointSummary("Login")]
    [EndpointDescription("Logs in a user")]
    public static async Task<Results<Ok<LoginResponseDto>, BadRequest, InternalServerError, UnauthorizedHttpResult>>
        Login(
            [FromBody] LoginRequestDto loginRequestDto, SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager, ITokenService tokenService
        )
    {
        try
        {
            var user = await userManager.FindByEmailAsync(loginRequestDto.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, loginRequestDto.Password))
            {
                return TypedResults.Unauthorized();
            }

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, loginRequestDto.Password, false);
            if (!signInResult.Succeeded)
            {
                return TypedResults.Unauthorized();
            }

            var tokens = await tokenService.CreateTokensAsync(user.Id.ToString());

            var response = new LoginResponseDto(tokens.AccessToken, tokens.RefreshToken);

            return TypedResults.Ok(response);
        }
        catch (Exception)
        {
            return TypedResults.InternalServerError();
        }
    }

    [EndpointSummary("Refresh")]
    [EndpointDescription("Gets a refresh token")]
    public static async Task<Results<Ok<GeneratedTokenDto>, UnauthorizedHttpResult>> Refresh(
        ITokenService tokenService, IUser currentUserService, [FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var userId = currentUserService.Id;
            if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();

            var response = await tokenService.RefreshTokenAsync(request.RefreshToken, new Guid(userId));

            return TypedResults.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
    }

    [EndpointSummary("Log out")]
    [EndpointDescription("Logs out the current user by clearing the authentication cookie.")]
    public static async Task<Results<Ok<LogoutResponseDto>, UnauthorizedHttpResult, InternalServerError>> Logout(
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService, IUser currentUserService,
        [FromBody] LogoutRequestDto request)
    {
        try
        {
            var userId = currentUserService.Id;
            if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();

            await tokenService.RevokeAsync(request.RefreshToken, new Guid(userId));

            return TypedResults.Ok(new LogoutResponseDto("Logged Out"));
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (Exception)
        {
            return TypedResults.InternalServerError();
        }
    }
}