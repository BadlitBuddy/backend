using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.RegisterUser;
using Api.Application.Users.Dtos;
using Api.Application.Users.Queries.GetCurrentUserDetails;
using Api.Application.Users.Queries.GetCurrentUserSubscriptionDetails;
using Api.Infrastructure.Identity;
using Api.Web.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NanoidDotNet;

namespace Api.Web.Endpoints;

public record LogoutResponseDto(string Message);

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("signup", Signup);
        groupBuilder.MapGet("auth/google-response-signup", SignupGoogleCallback);
        groupBuilder.MapGet("login", Login);
        groupBuilder.MapGet("auth/google-response-login", LoginGoogleCallback);
        groupBuilder.MapGet("me", GetUserDetails);
        groupBuilder.MapGet("me/organization", GetUserOrganization);
        groupBuilder.MapPost("refresh", Refresh);
        groupBuilder.MapPost("logout", Logout).RequireAuthorization();
    }

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

    private static RedirectHttpResult RedirectWithError(string baseUrl, string code, string message)
    {
        var url = QueryHelpers.AddQueryString(baseUrl, new Dictionary<string, string?>
        {
            ["error"] = code,
            ["error_description"] = message
        });
        return TypedResults.Redirect(url);
    }

    [EndpointSummary("Sign up")]
    [EndpointDescription("Signs up a user")]
    public static Results<ChallengeHttpResult, RedirectHttpResult> Signup([FromQuery] bool hasAcceptedTerms,
        IOptions<ClientOptions> clientOptions)
    {
        var clientDomain = clientOptions.Value.ClientDomain;
        try
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/users/auth/google-response-signup",
                Items =
                {
                    ["hasAcceptedTerms"] = hasAcceptedTerms.ToString(),
                    ["successRedirectUrl"] = $"{clientDomain}/login",
                    ["failureRedirectUrl"] = $"{clientDomain}/signup"
                }
            };
            return TypedResults.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        }
        catch (Exception)
        {
            return RedirectWithError($"{clientDomain}/signup", "Google authentication failed.",
                "An unexpected error occured while trying to authenticate with google, please try again.");
        }
    }

    [EndpointSummary("google callback")]
    [EndpointDescription("google auth callback")]
    public static async Task<RedirectHttpResult> SignupGoogleCallback(
        ISender sender, HttpContext context, UserManager<ApplicationUser> userManager, ITokenService tokenService,
        IWebHostEnvironment environment, IOptions<ClientOptions> clientOptions)
    {
        try
        {
            var clientDomain = clientOptions.Value.ClientDomain;
            var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return RedirectWithError($"{clientDomain}/signup", "Auth Failed",
                    "Google failed to authenticate your account.");
            }

            var items = result.Properties.Items;
            var successRedirectUrl = items["successRedirectUrl"];
            var failureRedirectUrl = items["failureRedirectUrl"];

            var hasAcceptedTerms = items["hasAcceptedTerms"];
            if (hasAcceptedTerms == null)
            {
                return RedirectWithError($"{failureRedirectUrl}", "Signup",
                    "Please accept the Terms of Service to sign up.");
            }

            var nameIdentifier = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var firstName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            var lastName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectWithError($"{failureRedirectUrl}", "Email", "Email not valid.");
            }

            if (!hasAcceptedTerms.Equals("TRUE", StringComparison.InvariantCultureIgnoreCase))
            {
                return RedirectWithError($"{failureRedirectUrl}", "Terms",
                    "You must accept the Terms of Service to sign up for an account.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var nanoId = Nanoid.Generate(size: 10);
                var userPassword = $"{email}-WS-{nameIdentifier}-{nanoId}";
                RegisterUserCommand command = new RegisterUserCommand
                {
                    Email = email,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName,
                    Password = userPassword,
                    HasAcceptedTerms = true
                };
                var signupResult = await sender.Send(command);
                if (signupResult.IsFailure)
                {
                    return RedirectWithError($"{failureRedirectUrl}", "User", "Failed to sign up the user");
                }
            }
            else
            {
                return RedirectWithError($"{failureRedirectUrl}", "User",
                    "Cannot sign up user, an existing user with the same email already exists.");
            }

            if (string.IsNullOrWhiteSpace(clientOptions.Value.ClientDomain))
            {
                return RedirectWithError($"{failureRedirectUrl}", "Client Domain",
                    "An internal server error has occured.");
            }

            return TypedResults.Redirect($"{successRedirectUrl}?signUpSuccess=true");
        }
        catch (Exception)
        {
            return RedirectWithError($"{clientOptions.Value.ClientDomain}/login", "Error",
                "An internal server error has occured.");
        }
    }

    [EndpointSummary("Login")]
    [EndpointDescription("Logs in a user")]
    public static Results<ChallengeHttpResult, RedirectHttpResult> Login(IOptions<ClientOptions> clientOptions)
    {
        var clientDomain = clientOptions.Value.ClientDomain;
        try
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/users/auth/google-response-login", Items =
                {
                    ["successRedirectUrl"] = $"{clientDomain}/dashboard",
                    ["failureRedirectUrl"] = $"{clientDomain}/login"
                }
            };
            return TypedResults.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        }
        catch (Exception)
        {
            return RedirectWithError($"{clientDomain}/login", "Google authentication failed.",
                "An unexpected error occured while trying to authenticate with google, please try again.");
        }
    }

    [EndpointSummary("google callback")]
    [EndpointDescription("google auth callback")]
    public static async Task<RedirectHttpResult> LoginGoogleCallback(
        ISender sender, HttpContext context, UserManager<ApplicationUser> userManager, ITokenService tokenService,
        IWebHostEnvironment environment, IOptions<ClientOptions> clientOptions, IOptions<JwtOptions> jwtOptions)
    {
        var clientDomain = clientOptions.Value.ClientDomain;

        try
        {
            var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return RedirectWithError($"{clientDomain}/signup", "Auth Failed",
                    "Google failed to authenticate your account.");
            }

            var items = result.Properties.Items;
            var successRedirectUrl = items["successRedirectUrl"];
            var failureRedirectUrl = items["failureRedirectUrl"];

            var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectWithError($"{failureRedirectUrl}", "Email",
                    "Email not provided by Google.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectWithError($"{failureRedirectUrl}", "Account not found",
                    "Please sign up for an account first.");
            }

            var tokens = await tokenService.CreateTokensAsync(user.Id.ToString());
            context.Response.Cookies.Append("AuthToken", tokens.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenExpirationMinutes ?? 5),
                Path = "/"
            });
            context.Response.Cookies.Append("RefreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays ?? 1),
                Path = "/api/users"
            });

            if (string.IsNullOrWhiteSpace(clientDomain))
            {
                return RedirectWithError($"{failureRedirectUrl}", "Client Domain",
                    "An internal server error has occured.");
            }

            return TypedResults.Redirect($"{successRedirectUrl}");
        }
        catch (Exception)
        {
            return RedirectWithError($"{clientDomain}/login", "Error",
                "An internal server error has occured.");
        }
    }

    [EndpointSummary("Get User Details")]
    [EndpointDescription("Gets the current users details")]
    public async static Task<Results<ProblemHttpResult, Ok<UserDetailsDto>>> GetUserDetails(
        IOptions<ClientOptions> clientOptions, ISender sender)
    {
        var result = await sender.Send(new GetCurrentUserDetailsQuery());
        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Get User Organization Details")]
    [EndpointDescription("Gets the current users organization details with subscription details")]
    public async static Task<Results<ProblemHttpResult, Ok<SubscriptionDetailsDto>>> GetUserOrganization(
        IOptions<ClientOptions> clientOptions, ISender sender)
    {
        var result = await sender.Send(new GetCurrentUserSubscriptionDetailsQuery());
        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Refresh")]
    [EndpointDescription("Gets a refresh token")]
    public static async Task<Results<Ok, ProblemHttpResult>> Refresh(
        ITokenService tokenService, IUser currentUserService, HttpContext context, IOptions<JwtOptions> jwtOptions)
    {
        try
        {
            var refreshToken = currentUserService.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return UnauthorizedProblem();
            }

            var response = await tokenService.RefreshTokensAsync(refreshToken);

            context.Response.Cookies.Append("AuthToken", response.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenExpirationMinutes ?? 5),
                Path = "/"
            });
            context.Response.Cookies.Append("RefreshToken", response.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays ?? 1),
                Path = "/api/users"
            });

            return TypedResults.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Unexpected problem");
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
            {
                return UnauthorizedProblem();
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return UnauthorizedProblem();
            }

            await tokenService.RevokeAsync(refreshToken, new Guid(userId));

            context.Response.Cookies.Delete("AuthToken");
            context.Response.Cookies.Delete("RefreshToken", new CookieOptions { Path = "/api/users" });

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
