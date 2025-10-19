using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Models;

[Entity("teams.account")]
public class TeamsAccountEntity() : Entity("teams.account")
{
    [JsonPropertyName("user")]
    public required Microsoft.Teams.Api.Account User { get; set; }
}