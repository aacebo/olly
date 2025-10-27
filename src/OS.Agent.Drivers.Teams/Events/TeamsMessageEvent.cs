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
        Tenant = @event.Tenant,
        Account = @event.Account,
        Install = @event.Install,
        Chat = @event.Chat,
        Message = @event.Message
    };
}