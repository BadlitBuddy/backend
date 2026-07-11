using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.RegisterUser;
using Api.Infrastructure.Identity;
using Api.Web.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Web.Endpoints;

public record LogoutResponseDto(string Message);

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("register", Register);
        groupBuilder.MapGet("auth/google-response-register", RegisterGoogleCallback);
        groupBuilder.MapGet("login", Login);
        groupBuilder.MapGet("auth/google-response-login", LoginGoogleCallback);
        groupBuilder.MapPost("refresh", Refresh).RequireAuthorization();
        groupBuilder.MapPost("logout", Logout).RequireAuthorization();
    }

    [EndpointSummary("Register")]
    [EndpointDescription("Registers a user")]
    public static Results<ChallengeHttpResult, InternalServerError> Register([FromQuery] bool hasAcceptedTerms)
    {
        try
        {
            var properties = new AuthenticationProperties
                { RedirectUri = $"/api/users/auth/google-response-register?hasAcceptedTerms={hasAcceptedTerms}" };
            return TypedResults.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        }
        catch (Exception)
        {
            return TypedResults.InternalServerError();
        }
    }

    [EndpointSummary("google callback")]
    [EndpointDescription("google auth callback")]
    public static async Task<Results<RedirectHttpResult, BadRequest<string>, InternalServerError>>
        RegisterGoogleCallback(
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
            var hasAcceptedTerms = context.Request.Query["hasAcceptedTerms"].ToString()
                .Equals("TRUE", StringComparison.InvariantCultureIgnoreCase);

            if (string.IsNullOrEmpty(email)) return TypedResults.BadRequest("Email not provided by Google.");
            if (!hasAcceptedTerms)
                return TypedResults.BadRequest("You must accept the Terms of Service to register an account.");

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var userPassword = $"{email}-WS-{nameIdentifier}";
                RegisterUserCommand command = new RegisterUserCommand
                {
                    Email = email,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName,
                    Password = userPassword,
                    HasAcceptedTerms = true
                };
                var registerResult = await sender.Send(command);
                if (registerResult.IsFailure)
                {
                    return TypedResults.InternalServerError();
                }
            }
            else
            {
                return TypedResults.BadRequest(
                    "Cannot register user, an existing user with the same email already exists");
            }

            if (string.IsNullOrWhiteSpace(clientOptions.Value.ClientDomain))
            {
                return TypedResults.InternalServerError();
            }

            return TypedResults.Redirect($"{clientOptions.Value.ClientDomain}/login");
        }
        catch (Exception)
        {
            return TypedResults.InternalServerError();
        }
    }

    [EndpointSummary("Login")]
    [EndpointDescription("Logs in a user")]
    public static Results<ChallengeHttpResult, InternalServerError> Login()
    {
        try
        {
            var properties = new AuthenticationProperties { RedirectUri = "/api/users/auth/google-response-login" };
            return TypedResults.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        }
        catch (Exception)
        {
            return TypedResults.InternalServerError();
        }
    }

    [EndpointSummary("google callback")]
    [EndpointDescription("google auth callback")]
    public static async Task<Results<RedirectHttpResult, BadRequest<string>, InternalServerError>> LoginGoogleCallback(
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

            var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return TypedResults.BadRequest("Email not provided by Google.");

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return TypedResults.BadRequest("Please register an account first.");
            }

            var tokens = await tokenService.CreateTokensAsync(user.Id.ToString());
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

            if (string.IsNullOrWhiteSpace(clientOptions.Value.ClientDomain))
            {
                return TypedResults.InternalServerError();
            }

            return TypedResults.Redirect($"{clientOptions.Value.ClientDomain}/dashboard");
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
