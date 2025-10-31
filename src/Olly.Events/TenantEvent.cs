using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class TenantEvent(ActionType action) : Event(EntityType.Tenant, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }
}