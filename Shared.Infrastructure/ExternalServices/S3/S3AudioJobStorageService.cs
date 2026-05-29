using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Abstractions.ExternalServices.S3;

namespace Shared.Infrastructure.ExternalServices.S3;

public class S3AudioJobStorageService : IAudioJobStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<S3Options> _options;
    private readonly ILogger<S3AudioJobStorageService> _logger;

    public S3AudioJobStorageService(IAmazonS3 s3Client, IOptions<S3Options> options, ILogger<S3AudioJobStorageService> logger)
    {
        _s3Client = s3Client;
        _options = options;
        _logger = logger;
    }
    
    public async Task<bool> IsStorageAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new HeadBucketRequest { BucketName = _options.Value.BucketName };
            await _s3Client.HeadBucketAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogCritical("Target S3 bucket {BucketName} does not exist.", _options.Value.BucketName);
            return false;
        }
    }

    public async Task<IEnumerable<AudioJobDto>> GetPendingJobsAsync(int batchSize, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _options.Value.BucketName,
            MaxKeys = batchSize
        };

        var response = await _s3Client.ListObjectsV2Async(request, cancellationToken);
        
        return response?.S3Objects == null ? [] : response.S3Objects.Select(s3Obj => new AudioJobDto(s3Obj.Key, s3Obj.Size));
    }

    public async Task<Stream> DownloadAudioAsync(string fileKey, CancellationToken cancellationToken)
    {
        var request = new GetObjectRequest
        {
            BucketName = _options.Value.BucketName,
            Key = fileKey
        };

        var response = await _s3Client.GetObjectAsync(request, cancellationToken);
        return response.ResponseStream;
    }

    public async Task<bool> DeleteAudioAsync(string fileKey, CancellationToken cancellationToken)
    {
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _options.Value.BucketName,
            Key = fileKey
        };

        try
        {
            var response = await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
        
            return response.HttpStatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.NoContent;
        }
        catch (AmazonS3Exception)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}