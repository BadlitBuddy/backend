namespace Infrastructure.Configuration;

public class S3Options
{
    public string? SecretKey { get; set; }
    public string? ApiUrl { get; set; }
    public string? AccountId { get; set; }
    public string? AccessKey { get; set; }
    public string? BucketName { get; set; }
}
