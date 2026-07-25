using Whisper.net;

namespace Shared.Infrastructure.Dtos.TranscriptionProviderResults;

public static class WhisperNetMapper
{
    public static TranscriptionResult ToResult(
        IEnumerable<SegmentData> segments)
    {
        var segmentList = segments.Select(ToSegment).ToList();
        var text = string.Concat(segmentList.Select(s => s.Text));
        var language = segmentList.FirstOrDefault()?.Language;
        TimeSpan? duration = segmentList.Count is 0
            ? null
            : segmentList.Max(s => s.End);

        return new TranscriptionResult
        {
            Text = text,
            Language = language,
            Duration = duration,
            Segments = segmentList,
            ProviderModelId = TranscriptionProvider.WhisperNet
        };
    }

    public static TranscriptionSegment ToSegment(SegmentData s) => new()
    {
        Text = s.Text,
        Start = s.Start,
        End = s.End,
        Language = s.Language,
        Probability = s.Probability,
        MinProbability = s.MinProbability,
        MaxProbability = s.MaxProbability,
        NoSpeechProbability = s.NoSpeechProbability,
        Tokens = s.Tokens?.Select(ToToken).ToArray() ?? []
    };

    public static TranscriptionToken ToToken(WhisperToken t) => new()
    {
        Id = t.Id,
        Text = t.Text,
        Start = t.Start > 0 ? TimeSpan.FromMilliseconds(t.Start) : null,
        End = t.End > 0 ? TimeSpan.FromMilliseconds(t.End) : null,
        TimestampId = t.TimestampId,
        Probability = t.Probability,
        LogProbability = t.ProbabilityLog,
        TimestampProbability = t.TimestampProbability,
        TimestampProbabilitySum = t.TimestampProbabilitySum,
        DtwTimestamp = t.DtwTimestamp,
        VoiceLength = t.VoiceLen
    };
}
