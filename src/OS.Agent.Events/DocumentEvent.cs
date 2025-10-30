using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class DocumentEvent(ActionType action) : Event(EntityType.Chat, action)
{
    [JsonPropertyName("record")]
    public required Record Record { get; init; }

    [JsonPropertyName("document")]
    public required Document Document { get; init; }

    [JsonPropertyName("tenant")]
    public Tenant? Tenant { get; init; }

    [JsonPropertyName("account")]
    public Account? Account { get; init; }

    [JsonPropertyName("install")]
    public Install? Install { get; init; }
}