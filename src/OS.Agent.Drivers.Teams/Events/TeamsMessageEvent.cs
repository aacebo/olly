using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Events;

public class TeamsMessageEvent(ActionType action, SourceType sourceType) : TeamsEvent(EntityType.Message, action, sourceType)
{
    public static TeamsMessageEvent From(MessageEvent @event) => new(@event.Action, @event.SourceType ?? @event.Message.SourceType)
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