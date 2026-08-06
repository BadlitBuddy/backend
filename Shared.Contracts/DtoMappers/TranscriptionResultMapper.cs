using System.Text;
using Shared.Contracts.Dtos;
using Shared.Contracts.Enums;

namespace Shared.Contracts.DtoMappers;

public static class TranscriptionResultMapper
{
    public static TranscriptionResult ToTranscriptionResult(
        this IEnumerable<TranscriptionSegment> segments,
        TranscriptionTask task = TranscriptionTask.Transcribe,
        TranscriptionProvider? providerModelId = null,
        string? language = null)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var segmentList = segments as IReadOnlyList<TranscriptionSegment> ?? segments.ToList();

        if (segmentList.Count == 0)
        {
            return new TranscriptionResult
            {
                Text = string.Empty,
                Task = task,
                ProviderModelId = providerModelId,
                Language = language,
                Segments = [],
                Words = [],
                Duration = TimeSpan.Zero
            };
        }

        var textBuilder = new StringBuilder();
        foreach (var segment in segmentList)
        {
            if (textBuilder.Length > 0 && !string.IsNullOrWhiteSpace(segment.Text))
            {
                textBuilder.Append(' ');
            }

            textBuilder.Append(segment.Text.Trim());
        }

        var allWords = segmentList
            .SelectMany(s => s.Words)
            .ToList();

        var totalDuration = segmentList.Max(s => s.End);

        var resolvedLanguage = language ?? segmentList.FirstOrDefault(s => !string.IsNullOrEmpty(s.Language))?.Language;

        return new TranscriptionResult
        {
            Text = textBuilder.ToString(),
            Language = resolvedLanguage,
            Duration = totalDuration,
            Task = task,
            ProviderModelId = providerModelId,
            Segments = segmentList,
            Words = allWords,
            WordCount = allWords.Count > 0 ? allWords.Count : null
        };
    }
}
