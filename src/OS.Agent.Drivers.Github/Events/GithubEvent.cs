using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Events;

public abstract class GithubEvent(EntityType type, ActionType action, SourceType sourceType) : Event(type, action)
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

    [JsonIgnore]
    public required IServiceScope Scope { get; init; }

    public Chat? GetChat()
    {
        return this is GithubInstallEvent install
            ? install.Chat
            : this is GithubMessageEvent message
                ? message.Chat
                : null;
    }

    public Message? GetMessage()
    {
        return this is GithubInstallEvent install
            ? install.Message
            : this is GithubMessageEvent message
                ? message.Message
                : null;
    }
}