using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Services;
using Shared.Contracts.Dtos;
using Shared.Infrastructure;
using Shared.Infrastructure.Configuration;
using Shared.Infrastructure.Jobs;
using Shared.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddConnectionStringsConfiguration(builder.Configuration)
    .AddS3Configuration(builder.Configuration)
    .AddS3Services()
    .AddHangFireStorage()
    .AddHangFireServerWorker()
    .AddRedisPublisherService()
    .AddDapperContext()
    .AddDataRepositories();

builder.Services.Configure<CloudflareOptions>(
    builder.Configuration.GetSection("Cloudflare")
);

builder.Services.AddHttpClient<CloudFlareWhisperClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CloudflareOptions>>().Value;
    client.BaseAddress = new Uri($"https://api.cloudflare.com/client/v4/accounts/{options.AccountId}/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
}).AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddSingleton<IStreamingTranscriptionService, WhisperTranscriptionService>();
builder.Services.AddSingleton<ITranscriptionService, CloudFlareWhisperTranscriptionService>();
builder.Services.AddSingleton<ITranscriptionExporter, TranscriptionExporter>();
builder.Services.AddSingleton<IStreamableTranscriptionExporter, StreamableTranscriptionExporter>();

builder.Services.AddScoped<ITranscriptionJob, TranscriptionJob>();

var host = builder.Build();
host.Run();
