using System.Text.Json;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[Entity("teams.chat")]
public class TeamsChatEntity : Entity
{
    public Microsoft.Teams.Api.Conversation Conversation
    {
        get => GetRequired<Microsoft.Teams.Api.Conversation>("conversation");
        set => Properties["conversation"] = JsonSerializer.SerializeToElement(value);
    }

    public string? ServiceUrl
    {
        get => Get<string>("service_url");
        set => Properties["service_url"] = JsonSerializer.SerializeToElement(value);
    }

    public TeamsChatEntity() : base("teams.chat")
    {

    }

    public TeamsChatEntity(Entity entity) : base(entity)
    {
        Type = "teams.chat";
    }
}

public static partial class EntityExtensions
{
    public static TeamsChatEntity AsTeamsChat(this Entity entity)
    {
        return new(entity);
    }
}