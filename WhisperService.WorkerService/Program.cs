using Amazon.Runtime;
using Amazon.S3;
using Infrastructure;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using WhisperService.Core.Services;
using WhisperService.WorkerService.Channels;
using WhisperService.WorkerService.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<S3Options>(
    builder.Configuration.GetSection("S3Options")
);

builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
builder.Services.AddSingleton<IAudioJobStorageService, S3AudioJobStorageService>();
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;
    var accountId = s3Options.AccountId ?? throw new ArgumentException("Missing account id config");
    var accessKey = s3Options.AccessKey ?? throw new ArgumentException("Missing access key config");
    var secretKey = s3Options.SecretKey ?? throw new ArgumentException("Missing secret key config");
    var credentials = new BasicAWSCredentials(accessKey, secretKey, accountId);
    
    var s3Config = new AmazonS3Config
    {
        ServiceURL = s3Options.ApiUrl ?? throw new ArgumentException("Missing ApiUrl config"),
        ForcePathStyle = true,
        UseHttp = false,
        AuthenticationRegion = "auto"
    };
    
    return new AmazonS3Client(credentials, s3Config);
});
builder.Services.AddSingleton<TranscriptionQueueChannel>();

builder.Services.AddHostedService<S3PollingWorker>();
builder.Services.AddHostedService<TranscriptionWorker>();

var host = builder.Build();
host.Run();
