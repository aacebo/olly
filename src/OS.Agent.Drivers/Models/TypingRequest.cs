using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class TypingRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("from")]
    public required Account From { get; set; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; set; }

    [JsonPropertyName("install")]
    public required Install Install { get; set; }
}