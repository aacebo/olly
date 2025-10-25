using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class TenantEvent(ActionType action) : Event(EntityType.Tenant, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }
}