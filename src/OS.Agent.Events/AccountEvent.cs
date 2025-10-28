using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public class AccountEvent(ActionType action) : Event(EntityType.Account, action)
{
    [JsonPropertyName("source_type")]
    public override SourceType? SourceType => Account.SourceType;

    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }
}