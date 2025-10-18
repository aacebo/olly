using System.Text.Json;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

public class GithubAccountEntity : Entity
{
    public Octokit.Webhooks.Models.User User
    {
        get => GetRequired<Octokit.Webhooks.Models.User>("user");
        set => Properties["user"] = JsonSerializer.SerializeToElement(value);
    }

    public GithubAccountEntity() : base("github.account")
    {

    }

    public GithubAccountEntity(Entity entity) : base(entity)
    {
        Type = "github.account";
    }
}

public static partial class EntityExtensions
{
    public static GithubAccountEntity AsGithubAccount(this Entity entity)
    {
        return new(entity);
    }
}