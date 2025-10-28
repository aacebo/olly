using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class ChatEvent(ActionType action) : Event(EntityType.Chat, action)
{
    [JsonPropertyName("source_type")]
    public override SourceType? SourceType => Chat.SourceType;

    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }
}