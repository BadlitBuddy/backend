using Infrastructure.Dtos.TranscriptionProviderResults;
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

    public async Task<TranscriptionResult> TranscribeAsync(string filePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        await using var fileStream = File.OpenRead(filePath);

        var request = new WhisperV3TurboRequest { Audio = null };
        var transcribeRequest = await _cloudFlareWhisperClient
            .TranscribeWhisperV3TurboAsync(fileStream, request, cancellationToken);

        var result = transcribeRequest?.Result;
        if (result == null)
        {
            throw new InvalidOperationException("Cloudflare Whisper transcription failed.");
        }

        return CloudflareMapper.ToResult(result);
    }
}
