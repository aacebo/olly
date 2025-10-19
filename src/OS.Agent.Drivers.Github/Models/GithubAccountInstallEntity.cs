using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[Entity("github.account.install")]
public class GithubAccountInstallEntity() : Entity("github.account.install")
{
    [JsonPropertyName("user")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.User>))]
    public required Octokit.User User { get; set; }

    [JsonPropertyName("install")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.Installation>))]
    public required Octokit.Installation Install { get; set; }

    [JsonPropertyName("access_token")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.AccessToken>))]
    public required Octokit.AccessToken AccessToken { get; set; }
}