using Api.Application.Common.Interfaces;
using Api.Web.Configuration;
using Api.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Web;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUserService>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi();
        
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public static void AddWebConfiguration(this IHostApplicationBuilder builder, IConfiguration configuration)
    {
        builder.Services.Configure<ClientOptions>(
            configuration.GetSection("ClientOptions")
        );
        builder.Services.Configure<AuthOptions>(
            configuration.GetSection("Auth")
        );
        builder.Services.Configure<JwtOptions>(
            configuration.GetSection("Jwt")
        );
    }
}
