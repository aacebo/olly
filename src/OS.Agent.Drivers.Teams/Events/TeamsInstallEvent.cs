using System.Text.Json.Serialization;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Events;

public class TeamsInstallEvent(ActionType action) : TeamsEvent(EntityType.Install, action)
{
    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    public static TeamsInstallEvent From(InstallEvent @event) => new(@event.Action)
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