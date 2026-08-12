namespace Aspire.Shared;

public static class Services
{
    /// <summary>
    /// The name of the main REST API service (Api.Web).
    /// </summary>
    public const string Api = "api";

    /// <summary>
    /// The name of the default Whisper transcription worker service.
    /// </summary>
    public const string WhisperServiceDefault = "whisper-service-default";

    /// <summary>
    /// The name of the Whisper transcription worker service that processed all public requests.
    /// Configured with a single worker and the `whisper-tiny-en` Hangfire queue.
    /// </summary>
    public const string WhisperServicePublic = "whisper-service-public";
}
