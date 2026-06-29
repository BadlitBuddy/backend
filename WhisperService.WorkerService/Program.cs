using Shared.Abstractions.Jobs;
using Shared.Abstractions.Services;
using Shared.Infrastructure;
using Shared.Infrastructure.Jobs;

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

builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();

builder.Services.AddScoped<ITranscriptionJob, TranscriptionJob>();

var host = builder.Build();
host.Run();
