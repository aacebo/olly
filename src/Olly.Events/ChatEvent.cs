using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class ChatEvent(ActionType action) : Event(EntityType.Chat, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }
}