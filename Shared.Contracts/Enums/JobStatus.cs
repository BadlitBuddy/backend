namespace Shared.Contracts.Enums;

using System.Text.Json.Serialization;

public class JobStatus
{
    public static readonly JobStatus Processing = new("Processing");
    public static readonly JobStatus Finished = new("Finished");

    public string Value { get; }

    [JsonConstructor] // Tells System.Text.Json to use this private constructor
    private JobStatus(string value) => Value = value;

    public override string ToString() => Value;
}
