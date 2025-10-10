using Octokit.Webhooks.Models;

namespace OS.Agent.Events;

public class GithubInstallEvent
{
    public required Installation Install { get; set; }
    public required Organization? Org { get; set; }
}