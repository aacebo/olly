using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class InstallEvent(ActionType action) : Event(EntityType.Install, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("install")]
    public required Install Install { get; init; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }

    [JsonPropertyName("message")]
    public Message? Message { get; init; }
}