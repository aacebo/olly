using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[Entity("github.user")]
public class GithubUserEntity() : Entity("github.user")
{
    [JsonPropertyName("user")]
    public required GithubUser User { get; set; }
}