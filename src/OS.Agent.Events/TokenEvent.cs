using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class TokenEvent
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("token")]
    public required Token Token { get; init; }
}