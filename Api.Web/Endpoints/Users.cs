using Api.Application.Common.Interfaces;
using Api.Application.Users.Commands.RegisterUser;
using Api.Domain.Entities;
using Api.Infrastructure.Identity;
using Api.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Web.Endpoints;

public record LoginRequestDto(string Email, string Password);
public record LoginResponseDto(string Token);

public class Users: IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Logout, "logout").RequireAuthorization();
    }
    
    [EndpointSummary("Register")]
    [EndpointDescription("Registers a new user")]
    public static async Task<Ok<string>> Register(
        ISender sender, RegisterUserCommand command
        )
    {
        var userId = await sender.Send(command);
        return TypedResults.Ok(userId);
    }
    
    [EndpointSummary("Login")]
    [EndpointDescription("Logs in a user")]
    public static async Task<Results<Ok<LoginResponseDto>, BadRequest, InternalServerError, UnauthorizedHttpResult>> Login(
        [FromBody] LoginRequestDto loginRequestDto, SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager, IJwtTokenGenerator tokenGenerator
    )
    {
        try
        {
            var user = await userManager.FindByEmailAsync(loginRequestDto.Email);
            if (user == null)
            {
                return TypedResults.BadRequest();
            }
            
            var signInResult = await  signInManager.CheckPasswordSignInAsync(user, loginRequestDto.Password, false);
            if (!signInResult.Succeeded)
            {
                return TypedResults.Unauthorized();
            }

            var userClaimsDto = new UserClaimsDto(user.Id, user.PublicId, user.Email);
            
            var accessToken =
                await tokenGenerator.CreateAccessTokenAsync(userClaimsDto);

            var response = new LoginResponseDto(accessToken);
        
            return TypedResults.Ok(response);
        }
        catch (Exception)
        {   
            return TypedResults.InternalServerError();
        }
    }
    
    [EndpointSummary("Log out")]
    [EndpointDescription("Logs out the current user by clearing the authentication cookie.")]
    public static async Task<Results<Ok, UnauthorizedHttpResult>> Logout(SignInManager<ApplicationUser> signInManager, [FromBody] object empty)
    {
        if (empty == null) return TypedResults.Unauthorized();
        
        await signInManager.SignOutAsync();
        return TypedResults.Ok();
    }
}
