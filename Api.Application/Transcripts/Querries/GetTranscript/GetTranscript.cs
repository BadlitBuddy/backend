using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Application.Common.Interfaces;
using Api.Application.Transcripts.Dtos;
using Shared.Abstractions.Services;
using Shared.Common.Helpers;
using Shared.Contracts.Dtos;

namespace Api.Application.Transcripts.Querries.GetTranscript;

public class GetTranscriptQuery : IRequest<Result<GetTranscriptResponse>>
{
    public string Id { get; set; } = string.Empty;
    public bool IncludeSrtSegments { get; set; } = false;
    public bool IncludeVttSegments { get; set; } = false;
}

public class GetTranscriptHandler : IRequestHandler<GetTranscriptQuery, Result<GetTranscriptResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAudioJobStorageService _audioJobStorage;
    private readonly ITranscriptionExporter _transcriptionExporter;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TimeSpanToSecondsJsonConverter()
        }
    };

    public GetTranscriptHandler(IApplicationDbContext dbContext, IAudioJobStorageService audioJobStorage,
        ITranscriptionExporter transcriptionExporter)
    {
        _dbContext = dbContext;
        _audioJobStorage = audioJobStorage;
        _transcriptionExporter = transcriptionExporter;
    }

    public async Task<Result<GetTranscriptResponse>> Handle(GetTranscriptQuery request, CancellationToken cancellationToken)
    {
        var transcript = await _dbContext.Transcripts
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.PublicId == request.Id, cancellationToken: cancellationToken);

        if (transcript == null)
        {
            return Result<GetTranscriptResponse>.Failure(["Transcript not found."]);
        }

        if (transcript.ProcessedObjectKey == null)
        {
            return Result<GetTranscriptResponse>.Failure(["Transcript must be processed first"]);
        }

        FileInfo transcriptResultFile = await _audioJobStorage.DownloadFileAsync(transcript.ProcessedObjectKey, cancellationToken);
        await using var stream = transcriptResultFile.OpenRead();
        var transcriptionResult = await JsonSerializer.DeserializeAsync<TranscriptionResult>(stream, JsonOptions, cancellationToken);

        if (transcriptionResult == null)
        {
            return Result<GetTranscriptResponse>.Failure(["Failed to load transcription."]);
        }

        if (transcriptResultFile.Exists)
        {
            transcriptResultFile.Delete();
        }

        var wordCount = transcriptionResult.WordCount > 0
            ? transcriptionResult.WordCount.Value
            : transcriptionResult.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return Result<GetTranscriptResponse>.Success(new GetTranscriptResponse
        {
            Transcript = transcript.ToDto(),
            JsonData = transcriptionResult,
            MetaData = new MetaData(wordCount, transcriptionResult.Duration.ToString() ?? "unknown",
                transcriptionResult.Language ?? "unknown"),
            SrtSegments = request.IncludeSrtSegments ? _transcriptionExporter.ToSrtSegments(transcriptionResult) : null,
            VttSegments = request.IncludeVttSegments ? _transcriptionExporter.ToVttSegments(transcriptionResult) : null
        });
    }
}
