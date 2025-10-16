using System.Text.Json;
using System.Text.Json.Serialization;

using OS.Agent.Json;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[JsonDerivedFromType(typeof(Data), "chat.teams")]
[JsonDerivedFromType(typeof(ChatData), "chat.teams")]
public class TeamsChatData : ChatData
{
    [JsonPropertyName("conversation")]
    public required Microsoft.Teams.Api.Conversation Conversation { get; set; }

    [JsonPropertyName("service_url")]
    public string? ServiceUrl { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}

public static partial class ChatDataExtensions
{
    public static TeamsChatData? Teams(this ChatData data)
    {
        return data as TeamsChatData;
    }
}