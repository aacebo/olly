using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

public class GithubAccountInstallEntity : Entity
{
    public Octokit.User User
    {
        get => GetRequired<Octokit.User>("user");
        set => Properties["user"] = GithubJsonSerializer.SerializeToElement(value);
    }

    public Octokit.Installation Install
    {
        get => GetRequired<Octokit.Installation>("install");
        set => Properties["install"] = GithubJsonSerializer.SerializeToElement(value);
    }

    public Octokit.AccessToken AccessToken
    {
        get => GetRequired<Octokit.AccessToken>("access_token");
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