using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Raffinert.FuzzySharp;
using Shared.Infrastructure.Helpers;

namespace Shared.Infrastructure.Services;

public static class TranscriptionResultMerger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TimeSpanToSecondsJsonConverter()
        },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { JsonTypeInfoResolvers.IgnoreProviderModelId }
        }
    };

    public static async Task<TranscriptionResult> MergeFilesAsync(
        IReadOnlyList<FileInfo> files, FileInfo? outputFile = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0) throw new ArgumentException("No chunk files provided.", nameof(files));

        var chunks = new List<TranscriptionResult>(files.Count);
        foreach (var file in files)
        {
            await using var stream = file.OpenRead();
            var chunk = await JsonSerializer.DeserializeAsync<TranscriptionResult>(stream, JsonOptions, ct);
            if (chunk != null)
            {
                chunks.Add(chunk);
            }
        }

        var merged = Merge(chunks);

        if (outputFile != null && outputFile.Exists)
        {
            outputFile.Directory?.Create();
            await using var outStream = outputFile.Create();
            await JsonSerializer.SerializeAsync(outStream, merged, JsonOptions, ct);
        }

        return merged;
    }

    public static TranscriptionResult Merge(IReadOnlyList<TranscriptionResult> chunks) =>
        chunks switch
        {
            [] => throw new ArgumentException("No chunks provided.", nameof(chunks)),
            [var single] => single with { Segments = [.. single.Segments.Select((s, i) => s with { Id = i })] },
            _ => chunks.Aggregate(MergeTwo)
        };

    private static TranscriptionResult MergeTwo(TranscriptionResult a, TranscriptionResult b)
    {
        if (a.Segments.Count == 0) return b;
        if (b.Segments.Count == 0) return a;

        var aTail = a.Segments.Select((seg, idx) => (Seg: seg, Index: idx)).TakeLast(30);
        var bHead = b.Segments.Select((seg, idx) => (Seg: seg, Index: idx)).Take(30);

        var best = (from itemA in aTail
                from itemB in bHead
                let score = Fuzz.WeightedRatio(itemA.Seg.Text, itemB.Seg.Text)
                where score >= 75
                orderby score descending
                select new { SegA = itemA.Seg, IndexA = itemA.Index, SegB = itemB.Seg, IndexB = itemB.Index }
            ).FirstOrDefault();

        var (cutA, skipB, timeOffset) = best != null
            ? (best.IndexA, best.IndexB + 1, best.SegA.Start - best.SegB.Start)
            : (a.Segments.Count - 1, 0, a.Duration ?? a.Segments[^1].End);

        if (timeOffset < TimeSpan.Zero) timeOffset = TimeSpan.Zero;

        // Slice & shift segments using LINQ Take & Skip on IReadOnlyList
        var keptA = a.Segments.Take(cutA + 1).ToList();
        var shiftedB = b.Segments.Skip(skipB).Select(s => s with
        {
            Start = s.Start + timeOffset,
            End = s.End + timeOffset,
            Words = [.. s.Words.Select(w => w with { Start = w.Start + timeOffset, End = w.End + timeOffset })],
            Tokens =
            [
                .. s.Tokens.Select(t => t with
                {
                    Start = t.Start != null ? t.Start + timeOffset : null,
                    End = t.End != null ? t.End + timeOffset : null
                })
            ]
        });

        var mergedSegments = keptA.Concat(shiftedB).Select((s, idx) => s with { Id = idx }).ToList();
        var fullText = string.Join(" ", mergedSegments.Select(s => s.Text.Trim())).Trim();

        var keptAEnd = keptA.Count > 0 ? keptA[^1].End : TimeSpan.Zero;
        var segBEnd = best != null ? best.SegB.End : TimeSpan.Zero;

        return a with
        {
            Text = fullText,
            Duration = mergedSegments.Count > 0 ? mergedSegments[^1].End : a.Duration,
            Segments = mergedSegments,
            Words =
            [
                .. a.Words.Where(w => w.End <= keptAEnd),
                .. b.Words.Where(w => w.Start >= segBEnd)
                    .Select(w => w with { Start = w.Start + timeOffset, End = w.End + timeOffset })
            ],
            WordCount = mergedSegments.Sum(s =>
                s.Words.Count > 0
                    ? s.Words.Count
                    : s.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length)
        };
    }
}
