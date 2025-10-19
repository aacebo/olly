using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[Entity("github.account.install")]
public class GithubAccountInstallEntity : Entity
{
    public Octokit.User User
    {
        get => GithubJsonSerializer.Deserialize<Octokit.User>(Get("user").GetRawText());
        set => Properties["user"] = GithubJsonSerializer.SerializeToElement(value);
    }

    public Octokit.Installation Install
    {
        get => GithubJsonSerializer.Deserialize<Octokit.Installation>(Get("install").GetRawText());
        set => Properties["install"] = GithubJsonSerializer.SerializeToElement(value);
    }

    public Octokit.AccessToken AccessToken
    {
        get => GithubJsonSerializer.Deserialize<Octokit.AccessToken>(Get("access_token").GetRawText());
        set => Properties["access_token"] = GithubJsonSerializer.SerializeToElement(value);
    }

    public GithubAccountInstallEntity() : base("github.account.install")
    {

    }

    public GithubAccountInstallEntity(Entity entity) : base(entity)
    {
        Type = "github.account.install";
    }
}

public static partial class EntityExtensions
{
    public static GithubAccountInstallEntity AsGithubAccountInstall(this Entity entity)
    {
        return new(entity);
    }
}