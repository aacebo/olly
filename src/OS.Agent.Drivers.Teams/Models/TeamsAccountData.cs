using System.Text.Json;
using System.Text.Json.Serialization;

using OS.Agent.Json;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[JsonDerivedFromType(typeof(Data), "account.teams")]
[JsonDerivedFromType(typeof(AccountData), "account.teams")]
public class TeamsAccountData : AccountData
{
    [JsonPropertyName("user")]
    public required Microsoft.Teams.Api.Account User { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}