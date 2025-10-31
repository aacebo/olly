using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Drivers.Github.Models;

[Entity("github.discussion")]
public class GithubDiscussionEntity() : Entity("github.discussion")
{
    [JsonPropertyName("discussion")]
    public required Octokit.Webhooks.Models.Discussion Discussion { get; set; }

    [JsonPropertyName("repository")]
    public required Octokit.Webhooks.Models.Repository Repository { get; set; }
}