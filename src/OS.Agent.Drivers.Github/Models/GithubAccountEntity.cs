using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[Entity("github.account")]
public class GithubAccountEntity() : Entity("github.account")
{
    [JsonPropertyName("user")]
    public required Octokit.Webhooks.Models.User User { get; set; }
}