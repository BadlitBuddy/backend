using System.Buffers.Binary;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NanoidDotNet;
using Shared.Abstractions.ExternalServices.S3;

namespace Shared.Infrastructure.ExternalServices.S3;

public class S3AudioJobStorageService : IAudioJobStorageService
{
    private const long MaxBytes = 25L * 1024 * 1024;
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<S3Options> _options;
    private readonly ILogger<S3AudioJobStorageService> _logger;

    public S3AudioJobStorageService(
        IAmazonS3 s3Client, IOptions<S3Options> options,
        ILogger<S3AudioJobStorageService> logger
    )
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

    public async Task<bool> IsWhisperCompatibleWavAsync(string fileKey, long? maxSizeBytes = 100L * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        if (maxSizeBytes.HasValue)
        {
            var metaReq = new GetObjectMetadataRequest
            {
                BucketName = _options.Value.BucketName,
                Key = fileKey
            };

            GetObjectMetadataResponse meta;
            try
            {
                meta = await _s3Client.GetObjectMetadataAsync(metaReq, cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            if (meta.ContentLength > maxSizeBytes.Value)
            {
                return false;
            }
        }

        try
        {
            var req = new GetObjectRequest
            {
                BucketName = _options.Value.BucketName,
                Key = fileKey,
                ByteRange = new ByteRange(0, 511)
            };

            using var resp = await _s3Client.GetObjectAsync(req, cancellationToken);
            await using var stream = resp.ResponseStream;

            using var br = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: false);

            if (new string(br.ReadChars(4)) != "RIFF") return false;
            br.ReadUInt32();
            if (new string(br.ReadChars(4)) != "WAVE") return false;

            while (stream.Position < stream.Length - 8)
            {
                string chunkId = new string(br.ReadChars(4));
                uint chunkSize = br.ReadUInt32();
                long nextChunk = stream.Position + chunkSize;

                if (chunkId == "fmt ")
                {
                    ushort formatTag = br.ReadUInt16();
                    ushort channels = br.ReadUInt16();
                    uint sampleRate = br.ReadUInt32();
                    br.ReadUInt32();
                    br.ReadUInt16();
                    ushort bitsPerSample = br.ReadUInt16();

                    return formatTag == 1 // PCM
                           && channels == 1 // mono
                           && sampleRate == 16000 // 16 kHz
                           && bitsPerSample == 16; // 16-bit
                }

                stream.Position = nextChunk;
            }

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<UploadUrlDto> CreateUploadUrlAsync(string userId, string originalFileName)
    {
        var fileExtension = Path.GetExtension(originalFileName);
        if (!string.Equals(fileExtension, ".wav",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only .wav files are supported.");
        }

        var shortId = await Nanoid.GenerateAsync(size: 10);
        var objectKey = $"{userId}/unprocessed/{shortId}-{Path.GetFileName(originalFileName)}";
        var request = new GetPreSignedUrlRequest()
        {
            BucketName = _options.Value.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            ContentType = "audio/wav",
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var response = await _s3Client.GetPreSignedURLAsync(request);
        return new UploadUrlDto
        {
            Url = response,
            ObjectKey = objectKey
        };
    }

    public async Task<IEnumerable<AudioJobDto>> GetPendingJobsAsync(int batchSize, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _options.Value.BucketName,
            MaxKeys = batchSize
        };

        var response = await _s3Client.ListObjectsV2Async(request, cancellationToken);

        return response?.S3Objects == null
            ? []
            : response.S3Objects.Select(s3Obj => new AudioJobDto(s3Obj.Key, s3Obj.Size));
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