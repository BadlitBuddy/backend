namespace Api.Domain.Enums;

public enum TranscriptionJobStatus
{
    [Description("Started")]
    Started,
    [Description("Processing")]
    Processing,
    [Description("Completed")]
    Completed,
}