using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class JobEvent(ActionType action) : Event(EntityType.Job, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("install")]
    public required Install Install { get; init; }

    [JsonPropertyName("user")]
    public required User User { get; init; }

    [JsonPropertyName("job")]
    public required Job Job { get; init; }
}