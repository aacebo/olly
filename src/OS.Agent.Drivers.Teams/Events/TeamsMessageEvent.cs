using System.Text.Json.Serialization;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Events;

public class TeamsMessageEvent(ActionType action) : TeamsEvent(EntityType.Message, action)
{
    [JsonPropertyName("message")]
    public required Message Message { get; init; }

    public static TeamsMessageEvent From(MessageEvent @event) => new(@event.Action)
    {
        Id = @event.Id,
        Type = @event.Type,
        Tenant = @event.Tenant,
        Account = @event.Account,
        Install = @event.Install,
        Chat = @event.Chat ?? throw new Exception("chat is required"),
        Message = @event.Message,
        CreatedBy = @event.CreatedBy,
        CreatedAt = @event.CreatedAt
    };
}