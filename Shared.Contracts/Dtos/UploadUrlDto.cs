namespace Shared.Contracts.Dtos;

public class UploadUrlDto
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Fields { get; set; } = new();
}