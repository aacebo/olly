using NetMQ;

using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Installation;
using Octokit.Webhooks.Models;

using OS.Agent.Models;

namespace OS.Agent.Webhooks;

public class InstallProcessor(ILogger<InstallProcessor> logger, NetMQQueue<Event<Installation>> events) : WebhookEventProcessor
{
    protected override ValueTask ProcessInstallationWebhookAsync
    (
        WebhookHeaders headers,
        InstallationEvent @event,
        InstallationAction action,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var ev = new Event<Installation>(
                action == InstallationAction.Created ? "github.install.create" : "github.install.delete",
                @event.Installation
            );

            Task.Run(() =>
            {
                events.Enqueue(ev);
                logger.LogDebug("[{}] => queued", ev.Name);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("{}", ex);
            throw new Exception("github.webhooks.install", ex);
        }

        return ValueTask.CompletedTask;
    }
}