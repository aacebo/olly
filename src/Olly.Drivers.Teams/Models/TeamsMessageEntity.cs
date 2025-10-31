using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Activities;

using Olly.Storage.Models;

namespace Olly.Drivers.Teams.Models;

[Entity("teams.message")]
public class TeamsMessageEntity() : Entity("teams.message")
{
    [JsonPropertyName("activity")]
    public required MessageActivity Activity { get; set; }
}