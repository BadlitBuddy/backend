using Whisper.net.Ggml;

// await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.LargeV3Turbo, QuantizationType.Q5_0);
await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Tiny);
await using var fileWriter = File.OpenWrite("tin.bin");
await modelStream.CopyToAsync(fileWriter);