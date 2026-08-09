using Infrastructure.Clients;
using Infrastructure.Dtos.TranscriptionProviderResults;
using Infrastructure.Dtos.TranscriptionProviderResults.CloudFlare;
using Shared.Abstractions.Services;
using Shared.Contracts.Dtos;
using WhisperService.Core.Dtos;

namespace Infrastructure.Services;

public class CloudFlareWhisperTranscriptionService : ITranscriptionService
{
    private readonly CloudFlareWhisperClient _cloudFlareWhisperClient;

    public CloudFlareWhisperTranscriptionService(CloudFlareWhisperClient cloudFlareWhisperClient)
    {
        _cloudFlareWhisperClient = cloudFlareWhisperClient;
    }

    public async Task<TranscriptionResult> TranscribeAsync(TranscriptionSource source,
        TranscriptionModel transcriptionModel,
        CancellationToken cancellationToken)
    {
        return source switch
        {
            TranscriptionSource.FilePath pathSource =>
                await TranscribeViaFilePathAsync(pathSource.FileInfo, transcriptionModel, cancellationToken),
            _ => throw new NotSupportedException(
                $"Audio source {source.GetType().Name} is not supported by this provider.")
        };
    }

    private async Task<TranscriptionResult> TranscribeViaFilePathAsync(FileInfo fileInfo,
        TranscriptionModel transcriptionModel,
        CancellationToken cancellationToken)
    {
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("The specified file was not found.", fileInfo.FullName);
        }

        await using var fileStream = fileInfo.OpenRead();
        return transcriptionModel switch
        {
            TranscriptionModel.WhisperV3LargeTurbo => await TranscribeWithWhisperV3Turbo(fileStream, cancellationToken),
            TranscriptionModel.WhisperTinyEn => await TranscribeWithWhisperTinyEn(fileStream, cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    private async Task<TranscriptionResult> TranscribeWithWhisperV3Turbo(FileStream fileStream,
        CancellationToken cancellationToken)
    {
        var request = new WhisperV3TurboRequest { Audio = null };
        var transcribeRequest = await _cloudFlareWhisperClient
            .TranscribeWhisperV3TurboAsync(fileStream, request, cancellationToken);

        var result = transcribeRequest?.Result;
        if (result == null)
        {
            throw new InvalidOperationException("Cloudflare Whisper transcription failed.");
        }

        return CfWhisperV3TurboResponseMapper.ToResult(result);
    }

    private async Task<TranscriptionResult> TranscribeWithWhisperTinyEn(FileStream fileStream,
        CancellationToken cancellationToken)
    {
        var transcribeRequest = await _cloudFlareWhisperClient
            .TranscribeWithWhisperTinyEnAsync(fileStream, cancellationToken);

        var result = transcribeRequest?.Result;
        if (result == null)
        {
            throw new InvalidOperationException("Cloudflare Whisper transcription failed.");
        }

        return CfWhisperTinyEnResponseMapper.ToResult(result);
    }
}
