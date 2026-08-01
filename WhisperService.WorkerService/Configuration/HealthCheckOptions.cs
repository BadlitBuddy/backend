namespace WhisperService.WorkerService.Configuration;

public class HealthCheckOptions
{
    public bool? EnableHealthCheck { get; set; }
    public int? Port { get; set; }
    public string? Path { get; set; }
}
