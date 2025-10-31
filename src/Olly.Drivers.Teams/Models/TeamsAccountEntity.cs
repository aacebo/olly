using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Drivers.Teams.Models;

[Entity("teams.account")]
public class TeamsAccountEntity() : Entity("teams.account")
{
    [JsonPropertyName("user")]
    public required Microsoft.Teams.Api.Account User { get; set; }
}