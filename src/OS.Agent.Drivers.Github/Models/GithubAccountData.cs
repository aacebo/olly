using System.Text.Json;
using System.Text.Json.Serialization;

using OS.Agent.Json;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[JsonDerivedFromType(typeof(Data), "account.github")]
[JsonDerivedFromType(typeof(AccountData), "account.github")]
public class GithubAccountData : AccountData
{
    [JsonPropertyName("user")]
    public required Octokit.Webhooks.Models.User User { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}