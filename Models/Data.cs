using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Activities;

namespace OS.Agent.Models;

public class Data
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public class Account : Data
    {
        public class Github : Account
        {
            [JsonPropertyName("user")]
            public required Octokit.Webhooks.Models.User User { get; set; }

            [JsonPropertyName("install")]
            public required Octokit.Webhooks.Models.Installation Install { get; set; }
        }

        public class Teams : Account
        {
            [JsonPropertyName("user")]
            public required Microsoft.Teams.Api.Account User { get; set; }
        }
    }

    public class Chat : Data
    {
        public class Teams : Chat
        {
            [JsonPropertyName("conversation")]
            public required Microsoft.Teams.Api.Conversation Conversation { get; set; }

            [JsonPropertyName("service_url")]
            public string? ServiceUrl { get; set; }
        }
    }

    public class Message : Data
    {
        public class Teams : Message
        {
            [JsonPropertyName("activity")]
            public required MessageActivity Activity { get; set; }
        }
    }
}