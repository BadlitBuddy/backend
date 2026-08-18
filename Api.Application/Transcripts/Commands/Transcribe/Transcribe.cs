using Api.Application.Common.Enums;
using Api.Application.Common.Interfaces;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Services;

namespace Api.Application.Transcripts.Commands.Transcribe;

public record TranscribedFileResponse(
    string Id,
    TranscriptionJobStatus JobStatus,
    TimeSpan Duration);

public class TranscribeFileCommand : IRequest<Result<TranscribedFileResponse>>
{
    public string? Id { get; set; }
    public required string UnprocessedObjectKey { get; set; }
}

public class TranscribeFileHandler : IRequestHandler<TranscribeFileCommand, Result<TranscribedFileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUser _currentUserService;
    private readonly ILogger<TranscribeFileHandler> _logger;
    private readonly ITranscriptionJobScheduler _transcriptionJobScheduler;
    private readonly IAudioJobStorageService _audioJobStorageService;

    public TranscribeFileHandler(IApplicationDbContext dbContext, IUser currentUserService,
        ITranscriptionJobScheduler transcriptionJobScheduler, ILogger<TranscribeFileHandler> logger,
        IAudioJobStorageService audioJobStorageService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _transcriptionJobScheduler = transcriptionJobScheduler;
        _logger = logger;
        _audioJobStorageService = audioJobStorageService;
    }

    public async Task<Result<TranscribedFileResponse>> Handle(TranscribeFileCommand request,
        CancellationToken cancellationToken)
    {
        var doesObjectKeyExist =
            await _audioJobStorageService.DoesObjectExistAsync(request.UnprocessedObjectKey, cancellationToken);
        if (!doesObjectKeyExist)
        {
            return Result<TranscribedFileResponse>.Failure(["Provided Object Key does not exist"]);
        }

        var userTier = ResolveTier();

        switch (userTier)
        {
            case UserTier.Free:
                var freeTranscriptionJob = await TranscribeFree(request, cancellationToken);
                return Result<TranscribedFileResponse>.Success(new TranscribedFileResponse(
                    freeTranscriptionJob.PublicId,
                    freeTranscriptionJob.JobStatus, freeTranscriptionJob.Duration));
            case UserTier.Public:
                var publicTranscriptionJob = await TranscribePublic(request, cancellationToken);
                return Result<TranscribedFileResponse>.Success(new TranscribedFileResponse(
                    publicTranscriptionJob.PublicId,
                    publicTranscriptionJob.JobStatus, publicTranscriptionJob.Duration));
            default:
                return Result<TranscribedFileResponse>.Failure(["Could not determine user tier"]);
        }
    }

    private UserTier ResolveTier()
    {
        var isAuthenticated = _currentUserService.IsAuthenticated;
        return isAuthenticated switch
        {
            true => UserTier.Free,
            false => UserTier.Public
        };
    }

    private async Task<Transcript> TranscribeFree(TranscribeFileCommand request,
        CancellationToken cancellationToken)
    {
        Transcript transcript;
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            var existingTranscriptionJob = await
                _dbContext.Transcripts.SingleOrDefaultAsync(tj => tj.PublicId == request.Id,
                    cancellationToken: cancellationToken);
            if (existingTranscriptionJob == null)
            {
                throw new NotFoundException("Transcript", "Transcript with provided Id cannot be fond");
            }

            transcript = existingTranscriptionJob;
        }
        else
        {
            transcript = new Transcript
            {
                UserId = Guid.Parse(_currentUserService.Id!),
                UnprocessedObjectKey = request.UnprocessedObjectKey
            };
            _dbContext.Transcripts.Add(transcript);
        }

        transcript.MarkAsProcessing();
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _transcriptionJobScheduler.EnqueueTranscriptionJob(
                request.UnprocessedObjectKey,
                cancellationToken);

            _logger.LogInformation("Transcription Job with key {ObjectKey}, has enqueued using Default Model",
                transcript.UnprocessedObjectKey);
        }
        catch (OperationCanceledException)
        {
            transcript.MarkAsCanceled();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Transcription Job with key {ObjectKey}, has been cancelled",
                transcript.UnprocessedObjectKey);
        }
        catch (Exception ex)
        {
            transcript.MarkAsFailed();
            await _dbContext.SaveChangesAsync(cancellationToken)
                ;
            _logger.LogError(
                "An unhandled error occurred while enqueuing Transcription Job with key {ObjectKey} with message {Message}",
                transcript.UnprocessedObjectKey, ex.Message);

            throw new InvalidOperationException(
                $"Failed to enqueue transcription job for key '{transcript.UnprocessedObjectKey}'.",
                ex);
        }

        return transcript;
    }

    private async Task<Transcript> TranscribePublic(TranscribeFileCommand request,
        CancellationToken cancellationToken)
    {
        Transcript transcript;
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            var existingTranscriptionJob = await
                _dbContext.Transcripts.SingleOrDefaultAsync(tj => tj.PublicId == request.Id,
                    cancellationToken: cancellationToken);
            if (existingTranscriptionJob == null)
            {
                throw new NotFoundException("Transcript", "Transcript with provided Id cannot be fond");
            }

            transcript = existingTranscriptionJob;
        }
        else
        {
            transcript = new Transcript
            {
                User = null,
                UserId = null,
                UnprocessedObjectKey = request.UnprocessedObjectKey
            };
            _dbContext.Transcripts.Add(transcript);
        }

        transcript.MarkAsProcessing();
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _transcriptionJobScheduler.EnqueueTranscriptionJobWithWhisperTinyEn(
                request.UnprocessedObjectKey,
                cancellationToken);

            _logger.LogInformation("Transcription Job with key {ObjectKey}, has enqueued using whisper-tiny-en model",
                transcript.UnprocessedObjectKey);
        }
        catch (OperationCanceledException)
        {
            transcript.MarkAsCanceled();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Transcription Job with key {ObjectKey}, has been cancelled",
                transcript.UnprocessedObjectKey);
        }
        catch (Exception ex)
        {
            transcript.MarkAsFailed();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                "An unhandled error occurred while enqueuing Transcription Job with key {ObjectKey} with message {Message}",
                transcript.UnprocessedObjectKey, ex.Message);

            throw new InvalidOperationException(
                $"Failed to enqueue transcription job for key '{transcript.UnprocessedObjectKey}'.",
                ex);
        }

        return transcript;
    }
}
