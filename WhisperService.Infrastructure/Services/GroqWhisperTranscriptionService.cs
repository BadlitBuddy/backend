using Infrastructure.Clients;
using Infrastructure.Dtos.TranscriptionProviderResults;
using Shared.Abstractions.Services;
using Shared.Contracts.Dtos;

namespace Infrastructure.Services;

public class GroqWhisperTranscriptionService : ITranscriptionService
{
    private readonly GroqWhisperClient _groqWhisperClient;

    public GroqWhisperTranscriptionService(GroqWhisperClient groqWhisperClient)
    {
        _groqWhisperClient = groqWhisperClient;
    }

    public async Task<TranscriptionResult> TranscribeAsync(TranscriptionSource source,
        CancellationToken cancellationToken = default)
    {
        return source switch
        {
            TranscriptionSource.Url pathSource =>
                await TranscribeViaUriAsync(pathSource.Uri, cancellationToken),
            _ => throw new NotSupportedException(
                $"Audio source {source.GetType().Name} is not supported by this provider.")
        };
    }

    private async Task<TranscriptionResult> TranscribeViaUriAsync(Uri uri,
        CancellationToken cancellationToken)
    {
        var result = await _groqWhisperClient.TranscribeWhisperV3TurboAsync(uri, cancellationToken);
        return GroqMapper.ToResult(result);
    }
}
