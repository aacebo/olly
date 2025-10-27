using System.Text.Json.Serialization;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Events;

public abstract class GithubEvent(EntityType type, ActionType action) : Event(type, action)
{
    [JsonPropertyName("key")]
    public override string Key => $"{SourceType}.{Type}.{Action}";

    [JsonPropertyName("source_type")]
    public SourceType SourceType { get; } = SourceType.Github;

    [JsonIgnore]
    public required IServiceScope Scope { get; init; }
}