using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class JobEvent(ActionType action) : Event(EntityType.Job, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("job")]
    public required Job Job { get; init; }
}