using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Activities;

using OS.Agent.Json;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[JsonDerivedFromType(typeof(Data), "message.teams")]
[JsonDerivedFromType(typeof(MessageData), "message.teams")]
public class TeamsMessageData : MessageData
{
    [JsonPropertyName("activity")]
    public required MessageActivity Activity { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}

public static partial class MessageDataExtensions
{
    public static TeamsMessageData? Teams(this MessageData data)
    {
        return data as TeamsMessageData;
    }
}