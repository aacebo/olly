using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class InstallEvent(ActionType action) : Event(EntityType.Install, action)
{
    [JsonPropertyName("source_type")]
    public override SourceType? SourceType => Message?.SourceType ?? Chat?.SourceType ?? Install.SourceType;

    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("install")]
    public required Install Install { get; init; }

    [JsonPropertyName("chat")]
    public Chat? Chat { get; init; }

    [JsonPropertyName("message")]
    public Message? Message { get; init; }
}