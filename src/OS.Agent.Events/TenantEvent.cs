using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class TenantEvent
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }
}