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
        TranscriptionModel transcriptionModel,
        CancellationToken cancellationToken = default)
    {
        return source switch
        {
            TranscriptionSource.Url pathSource =>
                await TranscribeViaUriAsync(pathSource.Uri, cancellationToken),
            TranscriptionSource.FilePath pathSource =>
                await TranscribeViaFilePathAsync(pathSource.FileInfo, cancellationToken),
            _ => throw new NotSupportedException(
                $"Audio source {source.GetType().Name} is not supported by this provider.")
        };
    }

    private async Task<TranscriptionResult> TranscribeViaUriAsync(Uri uri,
        CancellationToken cancellationToken)
    {
        var result =
            await _groqWhisperClient.TranscribeAsync(uri, GroqWhisperModels.WhisperV3LargeTurbo, cancellationToken);
        return GroqMapper.ToResult(result);
    }

    private async Task<TranscriptionResult> TranscribeViaFilePathAsync(FileInfo fileInfo,
        CancellationToken cancellationToken)
    {
        var result =
            await _groqWhisperClient.TranscribeAsync(fileInfo, GroqWhisperModels.WhisperV3Large,
                cancellationToken);
        return GroqMapper.ToResult(result);
    }
}
