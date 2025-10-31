using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class LogEvent(ActionType action) : Event(EntityType.Log, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("log")]
    public required Log Log { get; init; }
}