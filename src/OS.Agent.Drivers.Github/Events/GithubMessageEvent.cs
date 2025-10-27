using System.Text.Json.Serialization;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Events;

public class GithubMessageEvent(ActionType action) : GithubEvent(EntityType.Message, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("install")]
    public required Install Install { get; init; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }

    [JsonPropertyName("message")]
    public required Message Message { get; init; }

    public static GithubMessageEvent From(MessageEvent @event, IServiceScope scope) => new(@event.Action)
    {
        Tenant = @event.Tenant,
        Account = @event.Account,
        Install = @event.Install,
        Chat = @event.Chat,
        Message = @event.Message,
        Scope = scope
    };
}