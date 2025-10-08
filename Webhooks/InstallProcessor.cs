using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Installation;

namespace OS.Agent.Webhooks;

public class InstallProcessor(ILogger<InstallProcessor> logger) : WebhookEventProcessor
{
    protected override ValueTask ProcessInstallationWebhookAsync
    (
        WebhookHeaders headers,
        InstallationEvent @event,
        InstallationAction @action,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("hit...");
        return ValueTask.CompletedTask;
    }
}