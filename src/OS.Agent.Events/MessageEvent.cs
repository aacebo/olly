using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class MessageEvent(ActionType action) : Event(EntityType.Message, action)
{
    [JsonPropertyName("source_type")]
    public override SourceType? SourceType => Message.SourceType;

    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("install")]
    public required Install Install { get; init; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }

    [JsonPropertyName("message")]
    public required Message Message { get; init; }
}