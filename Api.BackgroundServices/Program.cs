using Api.BackgroundServices.Workers;
using Shared.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<S3PollerWorker>();
builder.Services
    .AddConnectionStringsConfiguration(builder.Configuration)
    .AddS3Configuration(builder.Configuration)
    .AddS3Services()
    .AddHangFireStorage();

var host = builder.Build();

host.Run();