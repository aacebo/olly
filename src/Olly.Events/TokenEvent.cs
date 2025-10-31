using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class TokenEvent(ActionType action) : Event(EntityType.Token, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("token")]
    public required Token Token { get; init; }
}