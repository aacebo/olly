using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[Entity("teams.chat")]
public class TeamsChatEntity() : Entity("teams.chat")
{
    [JsonPropertyName("conversation")]
    public required Microsoft.Teams.Api.Conversation Conversation { get; set; }

    [JsonPropertyName("service_url")]
    public string? ServiceUrl { get; set; }
}