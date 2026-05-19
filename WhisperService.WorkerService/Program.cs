using Infrastructure;
using WhisperService.Core.Services;
using WhisperService.WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
builder.Services.AddHostedService<TranscriptionWorker>();

var host = builder.Build();
host.Run();
