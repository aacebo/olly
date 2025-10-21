using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class SignInRequest
{
    [JsonPropertyName("from")]
    public required Account From { get; set; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("state")]
    public required string State { get; set; }
}