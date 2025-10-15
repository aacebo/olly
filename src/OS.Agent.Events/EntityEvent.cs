using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class EntityEvent
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("entity")]
    public required Entity Entity { get; init; }

    [JsonPropertyName("account")]
    public Account? Account { get; init; }
}