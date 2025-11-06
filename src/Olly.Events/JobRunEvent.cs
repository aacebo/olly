using System.Text.Json.Serialization;

using Olly.Storage.Models;
using Olly.Storage.Models.Jobs;

namespace Olly.Events;

public class JobRunEvent(ActionType action) : Event(EntityType.Run, action)
{
    [JsonPropertyName("attempt")]
    public int Attempt { get; set; } = 1;

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

    [JsonPropertyName("chat")]
    public Chat? Chat { get; init; }

    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    [JsonPropertyName("run")]
    public required Run Run { get; init; }
}