using System.Text.Json.Serialization;

using OS.Agent.Models;

namespace OS.Agent.Events;

public class ChatEvent
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }
}