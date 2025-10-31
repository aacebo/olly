using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Drivers.Github.Models;

[Entity("github.user")]
public class GithubUserEntity() : Entity("github.user")
{
    [JsonPropertyName("user")]
    public required GithubUser User { get; set; }
}