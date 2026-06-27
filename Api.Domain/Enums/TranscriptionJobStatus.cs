namespace Api.Domain.Enums;

public enum TranscriptionJobStatus
{
    [Description("Uploaded")]
    Uploaded,
    [Description("Processing")]
    Processing,
    [Description("Completed")]
    Completed
}