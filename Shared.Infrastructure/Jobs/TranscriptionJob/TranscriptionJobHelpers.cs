using Shared.Abstractions.Services;
using Shared.Contracts.Enums;

namespace Shared.Infrastructure.Jobs.TranscriptionJob;

public static class TranscriptionJobHelpers
{
    // *NOTE, using fileKey here to uniquely identify this job
    public static (string toProcessDir, string processedDir) CreateWorkingDirs(string fileKey)
    {
        string toProcessDir = Path.Combine(AppContext.BaseDirectory, "MediaFiles", "ToProcess", fileKey);
        string processedDir = Path.Combine(AppContext.BaseDirectory, "MediaFiles", "Processed", fileKey);
        Directory.CreateDirectory(toProcessDir);
        Directory.CreateDirectory(processedDir);

        return (toProcessDir, processedDir);
    }

    public static (string toProcessFilePath, string processedFilePath) CreateWorkingFilePaths(string toProcessDir,
        string processedDir, string fileKey)
    {
        string toProcessFilePath = Path.Combine(toProcessDir, Path.GetFileName(fileKey));
        string processedFileName = Path.ChangeExtension(Path.GetFileName(fileKey), ".json");
        string processedFilePath = Path.Combine(processedDir, processedFileName);

        return (toProcessFilePath, processedFilePath);
    }

    public static void CleanDirectory(DirectoryInfo directory)
    {
        if (!directory.Exists) return;

        foreach (FileInfo file in directory.EnumerateFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
        {
            subDirectory.Delete(recursive: true);
        }
    }

    public static async Task PublishProgressAsync(
        Task transcriptionTask,
        string unprocessedWavFileObjectKey,
        IMessagePublisher messagePublisher,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (transcriptionTask.IsCompleted)
                    break;

                await messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
                    new TranscriptionProcessMessage(JobStatus.Processing, unprocessedWavFileObjectKey, null));
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
