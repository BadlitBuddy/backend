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
        string clientDomain = builder.Configuration["ClientOptions:ClientDomain"] ?? "http://localhost:3000";

        var uriBuilder = new UriBuilder(clientDomain);
        uriBuilder.Scheme = Uri.UriSchemeHttp;
        var httpClientUrl = uriBuilder.Uri.ToString().TrimEnd('/');
        uriBuilder.Scheme = Uri.UriSchemeHttps;
        var httpsClientUrl = uriBuilder.Uri.ToString().TrimEnd('/');

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins(httpsClientUrl, httpClientUrl)
                    .AllowAnyHeader()
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
