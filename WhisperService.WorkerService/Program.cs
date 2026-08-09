using System.Net.Http.Headers;
using Infrastructure;
using Infrastructure.Clients;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;
using Shared.Infrastructure;
using Shared.Infrastructure.Configuration;
using Shared.Infrastructure.Jobs;
using Shared.Infrastructure.Services;
using TinyHealthCheck;
using WhisperService.WorkerService.Configuration;

var builder = Host.CreateApplicationBuilder(args);

var healthOptions = builder.Configuration
    .GetSection("HealthCheck")
    .Get<HealthCheckOptions>();

builder.Services.Configure<HealthCheckOptions>(
    builder.Configuration.GetSection("HealthCheck")
);

if (healthOptions?.EnableHealthCheck == true)
{
    builder.Services.AddBasicTinyHealthCheck(config =>
    {
        config.Port = healthOptions.Port ?? 8080;
        config.UrlPath = healthOptions.Path ?? "/healthz";
        config.Hostname = "*";
        return config;
    });
}

builder.Services
    .AddConnectionStringsConfiguration(builder.Configuration)
    .AddS3Configuration(builder.Configuration)
    .AddS3Services()
    .AddHangFireStorage()
    .AddHangFireServerWorker(builder.Configuration)
    .AddRedisPublisherService()
    .AddDapperContext()
    .AddDataRepositories();

builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection("TranscriptionWorker")
);
builder.Services.Configure<CloudflareOptions>(
    builder.Configuration.GetSection("Cloudflare")
);
builder.Services.Configure<GroqOptions>(
    builder.Configuration.GetSection("Groq")
);

builder.Services.ConfigureHttpClientDefaults(httpBuilder =>
{
    httpBuilder.AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 2;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
    });
});
builder.Services.AddHttpClient<CloudFlareWhisperClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CloudflareOptions>>().Value;
    client.BaseAddress = new Uri($"https://api.cloudflare.com/client/v4/accounts/{options.AccountId}/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
});
builder.Services.AddHttpClient<GroqWhisperClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GroqOptions>>().Value;
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
});

builder.Services.AddSingleton<IStreamingTranscriptionService, WhisperTranscriptionService>();
builder.Services.AddKeyedSingleton<ITranscriptionService, CloudFlareWhisperTranscriptionService>(TranscriptionProvider
    .Cloudflare);
builder.Services.AddKeyedSingleton<ITranscriptionService, GroqWhisperTranscriptionService>(TranscriptionProvider.Groq);
builder.Services.AddSingleton<ITranscriptionExporter, TranscriptionExporter>();
builder.Services.AddSingleton<IStreamableTranscriptionExporter, StreamableTranscriptionExporter>();

builder.Services.AddScoped<ITranscriptionJob, TranscriptionJob>();

var host = builder.Build();
host.Run();
