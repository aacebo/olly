using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[Entity("github.discussion")]
public class GithubDiscussionEntity : Entity
{
    public Octokit.Webhooks.Models.Discussion Discussion
    {
        get => GetRequired<Octokit.Webhooks.Models.Discussion>("discussion");
        set => this["discussion"] = GithubJsonSerializer.SerializeToElement(value);
    }

    public GithubDiscussionEntity() : base("github.discussion")
    {

    }

    public GithubDiscussionEntity(Entity entity) : base(entity)
    {
        Type = "github.discussion";
    }
}

public static partial class EntityExtensions
{
    public static GithubDiscussionEntity AsGithubDiscussion(this Entity entity)
    {
        return new(entity);
    }
}