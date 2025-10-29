using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class RecordEvent(ActionType action) : Event(EntityType.Record, action)
{
    [JsonPropertyName("record")]
    public required Record Record { get; init; }

    [JsonPropertyName("tenant")]
    public Tenant? Tenant { get; init; }

    [JsonPropertyName("account")]
    public Account? Account { get; init; }

    [JsonPropertyName("install")]
    public Install? Install { get; init; }

    [JsonPropertyName("chat")]
    public Chat? Chat { get; init; }

    [JsonPropertyName("message")]
    public Message? Message { get; init; }
}