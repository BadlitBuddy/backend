namespace Api.Domain.Enums;

public enum TranscriptionJobStatus
{
    [Description("Uploaded")]
    Uploaded = 0,
    [Description("Processing")]
    Processing = 1,
    [Description("Completed")]
    Completed = 2,
    [Description("Canceled")]
    Canceled = 3,
    [Description("Failed")]
    Failed = 4
}
