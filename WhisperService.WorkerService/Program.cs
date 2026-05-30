using Infrastructure;
using Shared.Infrastructure;
using WhisperService.Core.Services;
using WhisperService.WorkerService.Channels;
using WhisperService.WorkerService.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddS3Configuration(builder.Configuration)
    .AddS3Services();

builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
builder.Services.AddSingleton<TranscriptionQueueChannel>();

builder.Services.AddHostedService<S3PollingWorker>();
builder.Services.AddHostedService<TranscriptionWorker>();

var host = builder.Build();
host.Run();
