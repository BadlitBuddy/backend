using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.RegisterUser;
using Api.Infrastructure.Identity;
using Api.Web.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Api.Web.Endpoints;

public record LogoutResponseDto(string Message);

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("login", Login);
        groupBuilder.MapGet("auth/google-response", GoogleCallback);
        groupBuilder.MapPost("refresh", Refresh).RequireAuthorization();
        groupBuilder.MapPost("logout", Logout).RequireAuthorization();
    }

    [EndpointSummary("Login")]
    [EndpointDescription("Logs in a user")]
    public static Results<ChallengeHttpResult, InternalServerError> Login()
    {
        try
        {
            var properties = new AuthenticationProperties { RedirectUri = "/api/users/auth/google-response" };
            return TypedResults.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        }
        catch (Exception)
        {
            return TypedResults.InternalServerError();
        }
    }

    [EndpointSummary("google callback")]
    [EndpointDescription("google auth callback")]
    public static async Task<Results<RedirectHttpResult, BadRequest<string>, InternalServerError>> GoogleCallback(
        ISender sender, HttpContext context, UserManager<ApplicationUser> userManager, ITokenService tokenService,
        IWebHostEnvironment environment, IOptions<ClientOptions> clientOptions)
    {
        try
        {
            var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return TypedResults.BadRequest("Google authentication failed.");
            }

            var nameIdentifier = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var firstName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            var lastName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;

            if (string.IsNullOrEmpty(email)) return TypedResults.BadRequest("Email not provided by Google.");

            var user = await userManager.FindByEmailAsync(email);
            string userId;
            if (user == null)
            {
                RegisterUserCommand command = new RegisterUserCommand
                {
                    Email = email,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName,
                    Password = nameIdentifier ?? Guid.CreateVersion7().ToString()
                };
                var registerResult = await sender.Send(command);
                if (registerResult.IsFailure)
                {
                    return TypedResults.InternalServerError();
                }

                userId = registerResult.Value;
            }
            else
            {
                userId = user.Id.ToString();
            }

            var tokens = await tokenService.CreateTokensAsync(userId);
            context.Response.Cookies.Append("AuthToken", tokens.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
            context.Response.Cookies.Append("RefreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/api/Users"
            });

            if (environment.IsDevelopment())
            {
                return TypedResults.Redirect("https://localhost:7168/scalar");
            }

            if (string.IsNullOrWhiteSpace(clientOptions.Value.ClientDomain))
            {
                return TypedResults.InternalServerError();
            }

            return TypedResults.Redirect($"{clientOptions.Value.ClientDomain}");
        }
        catch (Exception)
        {
            return TypedResults.InternalServerError();
        }
    }

    [EndpointSummary("Refresh")]
    [EndpointDescription("Gets a refresh token")]
    public static async Task<Results<Ok, UnauthorizedHttpResult>> Refresh(
        ITokenService tokenService, IUser currentUserService, HttpContext context)
    {
        try
        {
            var userId = currentUserService.Id;
            var refreshToken = currentUserService.RefreshToken;
            if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();
            if (string.IsNullOrWhiteSpace(refreshToken)) return TypedResults.Unauthorized();

            var response = await tokenService.RefreshTokenAsync(refreshToken, new Guid(userId));

            context.Response.Cookies.Append("AuthToken", response.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
            context.Response.Cookies.Append("RefreshToken", response.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/api/Users"
            });

            return TypedResults.Ok();
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
        ITokenService tokenService, IUser currentUserService, HttpContext context)
    {
        try
        {
            var userId = currentUserService.Id;
            var refreshToken = currentUserService.RefreshToken;
            if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Unauthorized();
            if (string.IsNullOrWhiteSpace(refreshToken)) return TypedResults.Unauthorized();

            await tokenService.RevokeAsync(refreshToken, new Guid(userId));

            context.Response.Cookies.Delete("AuthToken");
            context.Response.Cookies.Delete("RefreshToken", new CookieOptions { Path = "/api/Users" });

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
