using System.Net;
using System.Text.Json;
using Shared.Infrastructure.Services;
using WhisperService.Core.Dtos;

namespace Infrastructure.Helpers;

public sealed class WhisperStreamingJsonContent : HttpContent
{
    private readonly WhisperV3TurboRequest _request;
    private readonly Stream _audioStream;

    public WhisperStreamingJsonContent(WhisperV3TurboRequest request, Stream audioStream)
    {
        _request = request;
        _audioStream = audioStream;
        Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    }

    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }

    protected override async Task SerializeToStreamAsync(
        Stream stream, TransportContext? context,
        CancellationToken cancellationToken)
    {
        using var prefixStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(prefixStream))
        {
            writer.WriteStartObject();

            writer.WriteString("task", _request.Task);

            if (_request.Language is not null)
                writer.WriteString("language", _request.Language);

            writer.WriteBoolean("vad_filter", _request.VadFilter);

            if (_request.InitialPrompt is not null)
                writer.WriteString("initial_prompt", _request.InitialPrompt);

            if (_request.Prefix is not null)
                writer.WriteString("prefix", _request.Prefix);

            writer.WriteNumber("beam_size", _request.BeamSize);
            writer.WriteBoolean("condition_on_previous_text", _request.ConditionOnPreviousText);
            writer.WriteNumber("no_speech_threshold", _request.NoSpeechThreshold);
            writer.WriteNumber("compression_ratio_threshold", _request.CompressionRatioThreshold);
            writer.WriteNumber("log_prob_threshold", _request.LogProbThreshold);

            if (_request.HallucinationSilenceThreshold.HasValue)
                writer.WriteNumber("hallucination_silence_threshold",
                    _request.HallucinationSilenceThreshold.Value);

            writer.WritePropertyName("audio");
            writer.Flush();
        }

        prefixStream.WriteByte((byte)'"');

        await stream.WriteAsync(prefixStream.GetBuffer().AsMemory(0, (int)prefixStream.Length), cancellationToken);

        await foreach (var chunk in MediaFilePreprocessor.EncodeToUtf8Async(_audioStream,
                           cancellationToken: cancellationToken))
        {
            try
            {
                await stream.WriteAsync(chunk.Memory, cancellationToken);
            }
            finally
            {
                chunk.Return();
            }
        }

        await stream.WriteAsync("\"}"u8.ToArray(), cancellationToken);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);
}
