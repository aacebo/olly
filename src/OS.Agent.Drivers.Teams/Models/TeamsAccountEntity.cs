using System.Text.Json;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[Entity("teams.account")]
public class TeamsAccountEntity : Entity
{
    public Microsoft.Teams.Api.Account User
    {
        get => GetRequired<Microsoft.Teams.Api.Account>("user");
        set => Properties["user"] = JsonSerializer.SerializeToElement(value);
    }

    public TeamsAccountEntity() : base("teams.account")
    {

    }

    public TeamsAccountEntity(Entity entity) : base(entity)
    {
        Type = "teams.account";
    }
}

public static partial class EntityExtensions
{
    public static TeamsAccountEntity AsTeamsAccount(this Entity entity)
    {
        return new(entity);
    }
}