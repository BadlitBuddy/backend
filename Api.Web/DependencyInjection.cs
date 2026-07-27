using System.Threading.RateLimiting;
using Api.Application.Common.Interfaces;
using Api.Web.Configuration;
using Api.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
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
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientId = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientId,
                    partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            //TODO: set this up in the future
            // options.AddPolicy("AuthPolicy", context =>
            // {
            //     var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            //     return RateLimitPartition.GetFixedWindowLimiter(
            //         partitionKey: $"auth_{clientId}",
            //         partition => new FixedWindowRateLimiterOptions
            //         {
            //             PermitLimit = 5,
            //             Window = TimeSpan.FromMinutes(1)
            //         });
            // });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.", cancellationToken);
            };
        });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
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
