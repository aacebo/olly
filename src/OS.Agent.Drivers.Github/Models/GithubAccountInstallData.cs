using System.Text.Json;
using System.Text.Json.Serialization;

using OS.Agent.Json;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[JsonDerivedFromType(typeof(Data), "account.github.install")]
[JsonDerivedFromType(typeof(AccountData), "account.github.install")]
public class GithubAccountInstallData : AccountData
{
    [JsonConverter(typeof(GithubJsonConverter<Octokit.User>))]
    [JsonPropertyName("user")]
    public required Octokit.User User { get; set; }

    [JsonConverter(typeof(GithubJsonConverter<Octokit.Installation>))]
    [JsonPropertyName("install")]
    public required Octokit.Installation Install { get; set; }

    [JsonConverter(typeof(GithubJsonConverter<Octokit.AccessToken>))]
    [JsonPropertyName("access_token")]
    public required Octokit.AccessToken AccessToken { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}