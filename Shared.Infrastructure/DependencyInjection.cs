using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Abstractions.ExternalServices.S3;
using Shared.Infrastructure.ExternalServices.S3;

namespace Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddS3Configuration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3Options>(
            configuration.GetSection("S3Options")
        );
        
        return services;
    }
    
    public static IServiceCollection AddS3Services(this IServiceCollection services)
    {
        services.AddSingleton<IAudioJobStorageService, S3AudioJobStorageService>();
        services.AddSingleton<IAmazonS3>(sp =>
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
        
        return services;
    }
}