using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class InstallRequest
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; set; }

    [JsonPropertyName("account")]
    public required Account Account { get; set; }

    [JsonPropertyName("install")]
    public required Install Install { get; set; }

    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}