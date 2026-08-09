using Hangfire;
using Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Constants;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHangFireServerWorker(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HangfireServerOptions>(
            configuration.GetSection("HangfireServer")
        );

        var hangfireOptions = configuration.GetSection("HangfireServer").Get<HangfireServerOptions>();

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireOptions?.WorkerCount ?? 1;
            options.Queues = hangfireOptions?.Queues ?? [HangfireQueueConstants.Default];
        });

        return services;
    }
}
