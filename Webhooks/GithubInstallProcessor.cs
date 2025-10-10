using NetMQ;

using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Installation;

using OS.Agent.Events;
using OS.Agent.Models;

namespace OS.Agent.Webhooks;

public class GithubInstallProcessor(ILogger<GithubInstallProcessor> logger, NetMQQueue<Event<GithubInstallEvent>> events) : WebhookEventProcessor
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
            var ev = new Event<GithubInstallEvent>(
                action == InstallationAction.Created ? "github.install.create" : "github.install.delete",
                new()
                {
                    Install = @event.Installation,
                    Org = @event.Organization
                }
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