using System.Text.Json.Serialization;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Events;

public abstract class TeamsEvent(EntityType type, ActionType action, SourceType sourceType) : Event(type, action)
{
    [JsonPropertyName("key")]
    public override string Key => $"{SourceType}.{Type}.{Action}";

    [JsonPropertyName("source_type")]
    public override SourceType SourceType { get; } = sourceType;

    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("install")]
    public required Install Install { get; init; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }

    public Message? GetMessage()
    {
        return this is TeamsInstallEvent install
            ? install.Message
            : this is TeamsMessageEvent message
                ? message.Message
                : null;
    }
}