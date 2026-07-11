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

    private static ProblemHttpResult BadRequestProblem(string detail, string title = "Bad Request") =>
        TypedResults.Problem(
            detail: detail,
            title: title,
            statusCode: StatusCodes.Status400BadRequest,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");

    private static ProblemHttpResult UnauthorizedProblem(string detail = "Authentication is required.") =>
        TypedResults.Problem(
            detail: detail,
            title: "Unauthorized",
            statusCode: StatusCodes.Status401Unauthorized,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.2");

    private static ProblemHttpResult InternalErrorProblem(string detail = "An unexpected error occurred.") =>
        TypedResults.Problem(
            detail: detail,
            title: "Internal Server Error",
            statusCode: StatusCodes.Status500InternalServerError,
            type: "https://tools.ietf.org/html/rfc9110#section-15.6.1");

    [EndpointSummary("Register")]
    [EndpointDescription("Registers a user")]
    public static Results<ChallengeHttpResult, ProblemHttpResult> Register([FromQuery] bool hasAcceptedTerms)
    {
        try
        {
            var properties = new AuthenticationProperties
                { RedirectUri = $"/api/users/auth/google-response-register?hasAcceptedTerms={hasAcceptedTerms}" };
            return TypedResults.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        }
        catch (Exception)
        {
            return InternalErrorProblem("Failed to initiate registration.");
        }
    }

    [EndpointSummary("google callback")]
    [EndpointDescription("google auth callback")]
    public static async Task<Results<RedirectHttpResult, ProblemHttpResult>> RegisterGoogleCallback(
        ISender sender, HttpContext context, UserManager<ApplicationUser> userManager, ITokenService tokenService,
        IWebHostEnvironment environment, IOptions<ClientOptions> clientOptions)
    {
        try
        {
            var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return BadRequestProblem("Google authentication failed.");
            }

            var nameIdentifier = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var firstName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            var lastName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;
            var hasAcceptedTerms = context.Request.Query["hasAcceptedTerms"].ToString()
                .Equals("TRUE", StringComparison.InvariantCultureIgnoreCase);

            if (string.IsNullOrEmpty(email))
                return BadRequestProblem("Email not provided by Google.");

            if (!hasAcceptedTerms)
                return BadRequestProblem(
                    "You must accept the Terms of Service to register an account.",
                    title: "Terms Not Accepted");

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
                    return InternalErrorProblem("Failed to register the user.");
                }
            }
            else
            {
                return BadRequestProblem(
                    "Cannot register user, an existing user with the same email already exists.",
                    title: "User Already Exists");
            }

            if (string.IsNullOrWhiteSpace(clientOptions.Value.ClientDomain))
            {
                return InternalErrorProblem("Client domain is not configured.");
            }

            return TypedResults.Redirect($"{clientOptions.Value.ClientDomain}/login");
        }
        catch (Exception)
        {
            return InternalErrorProblem();
        }
    }

    [EndpointSummary("Login")]
    [EndpointDescription("Logs in a user")]
    public static Results<ChallengeHttpResult, ProblemHttpResult> Login()
    {
        try
        {
            var properties = new AuthenticationProperties { RedirectUri = "/api/users/auth/google-response-login" };
            return TypedResults.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        }
        catch (Exception)
        {
            return InternalErrorProblem("Failed to initiate login.");
        }
    }

    [EndpointSummary("google callback")]
    [EndpointDescription("google auth callback")]
    public static async Task<Results<RedirectHttpResult, ProblemHttpResult>> LoginGoogleCallback(
        ISender sender, HttpContext context, UserManager<ApplicationUser> userManager, ITokenService tokenService,
        IWebHostEnvironment environment, IOptions<ClientOptions> clientOptions)
    {
        try
        {
            var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return BadRequestProblem("Google authentication failed.");
            }

            var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return BadRequestProblem("Email not provided by Google.");

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequestProblem("Please register an account first.", title: "Account Not Found");
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
                return InternalErrorProblem("Client domain is not configured.");
            }

            return TypedResults.Redirect($"{clientOptions.Value.ClientDomain}/dashboard");
        }
        catch (Exception)
        {
            return InternalErrorProblem();
        }
    }

    [EndpointSummary("Refresh")]
    [EndpointDescription("Gets a refresh token")]
    public static async Task<Results<Ok, ProblemHttpResult>> Refresh(
        ITokenService tokenService, IUser currentUserService, HttpContext context)
    {
        try
        {
            var userId = currentUserService.Id;
            var refreshToken = currentUserService.RefreshToken;
            if (string.IsNullOrWhiteSpace(userId))
                return UnauthorizedProblem();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return UnauthorizedProblem();

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
            return UnauthorizedProblem();
        }
    }

    [EndpointSummary("Log out")]
    [EndpointDescription("Logs out the current user by clearing the authentication cookie.")]
    public static async Task<Results<Ok<LogoutResponseDto>, ProblemHttpResult>> Logout(
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService, IUser currentUserService, HttpContext context)
    {
        try
        {
            var userId = currentUserService.Id;
            var refreshToken = currentUserService.RefreshToken;
            if (string.IsNullOrWhiteSpace(userId))
                return UnauthorizedProblem();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return UnauthorizedProblem();

            await tokenService.RevokeAsync(refreshToken, new Guid(userId));

            context.Response.Cookies.Delete("AuthToken");
            context.Response.Cookies.Delete("RefreshToken", new CookieOptions { Path = "/api/Users" });

            return TypedResults.Ok(new LogoutResponseDto("Logged Out"));
        }
        catch (UnauthorizedAccessException)
        {
            return UnauthorizedProblem();
        }
        catch (Exception)
        {
            return InternalErrorProblem();
        }
    }
}
