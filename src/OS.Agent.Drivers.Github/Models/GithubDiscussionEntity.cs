using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[Entity("github.discussion")]
public class GithubDiscussionEntity() : Entity("github.discussion")
{
    [JsonPropertyName("discussion")]
    public required Octokit.Webhooks.Models.Discussion Discussion { get; set; }
}