using System.Text.Json.Serialization;

using Olly.Drivers.Github.Json;
using Olly.Storage.Models;

namespace Olly.Drivers.Github.Models;

[Entity("github.install")]
public class GithubInstallEntity() : Entity("github.install")
{
    [JsonPropertyName("install")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.Installation>))]
    public required Octokit.Installation Install { get; set; }

    [JsonPropertyName("access_token")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.AccessToken>))]
    public required Octokit.AccessToken AccessToken { get; set; }
}