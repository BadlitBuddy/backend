namespace Api.Web.Configuration;

public class AuthOptions
{
    public Google Google { get; set; } = new();
}

public class Google
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}