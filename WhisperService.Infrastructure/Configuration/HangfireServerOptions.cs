namespace Infrastructure.Configuration;

public class HangfireServerOptions
{
    public int? WorkerCount { get; set; }
    public string[]? Queues { get; set; }
}
