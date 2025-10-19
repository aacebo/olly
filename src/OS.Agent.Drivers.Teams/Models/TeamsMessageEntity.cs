using System.Text.Json;

using Microsoft.Teams.Api.Activities;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[Entity("teams.message")]
public class TeamsMessageEntity : Entity
{
    public MessageActivity Activity
    {
        get => GetRequired<MessageActivity>("activity");
        set => Properties["activity"] = JsonSerializer.SerializeToElement(value);
    }

    public TeamsMessageEntity() : base("teams.message")
    {

    }

    public TeamsMessageEntity(Entity entity) : base(entity)
    {
        Type = "teams.message";
    }
}

public static partial class EntityExtensions
{
    public static TeamsMessageEntity AsTeamsMessage(this Entity entity)
    {
        return new(entity);
    }
}