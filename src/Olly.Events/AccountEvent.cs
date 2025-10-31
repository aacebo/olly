using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class AccountEvent(ActionType action) : Event(EntityType.Account, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }
}