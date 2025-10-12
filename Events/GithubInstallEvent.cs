using System.Text.Json.Serialization;

using Octokit.Webhooks.Models;

namespace OS.Agent.Events;

public class GithubInstallEvent
{
    [JsonPropertyName("install")]
    public required Installation Install { get; set; }

    [JsonPropertyName("org")]
    public required Organization? Org { get; set; }
}