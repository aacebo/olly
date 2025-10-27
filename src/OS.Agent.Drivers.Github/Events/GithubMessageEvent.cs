using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Events;

public class GithubMessageEvent(ActionType action) : GithubEvent(EntityType.Message, action)
{
    [JsonPropertyName("chat")]
    public required Chat Chat { get; init; }

    [JsonPropertyName("message")]
    public required Message Message { get; init; }

    public static GithubMessageEvent From(MessageEvent @event, IServiceScope scope) => new(@event.Action)
    {
        Id = @event.Id,
        Type = @event.Type,
        Tenant = @event.Tenant,
        Account = @event.Account,
        Install = @event.Install,
        Chat = @event.Chat ?? throw new Exception("chat is required"),
        Message = @event.Message,
        Scope = scope,
        CreatedBy = @event.CreatedBy,
        CreatedAt = @event.CreatedAt
    };
}