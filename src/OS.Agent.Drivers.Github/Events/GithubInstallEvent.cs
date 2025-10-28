using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Events;

public class GithubInstallEvent(ActionType action, SourceType sourceType) : GithubEvent(EntityType.Install, action, sourceType)
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; init; }

    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    public static GithubInstallEvent From(InstallEvent @event, IServiceScope scope) => new(@event.Action, @event.SourceType ?? @event.Install.SourceType)
    {
        Id = @event.Id,
        Type = @event.Type,
        Tenant = @event.Tenant,
        Account = @event.Account,
        Install = @event.Install,
        Chat = @event.Chat,
        Message = @event.Message,
        Scope = scope,
        CreatedBy = @event.CreatedBy,
        CreatedAt = @event.CreatedAt
    };
}