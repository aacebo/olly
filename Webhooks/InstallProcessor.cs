using Microsoft.Extensions.Options;

using NetMQ;
using NetMQ.Sockets;

using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Installation;
using Octokit.Webhooks.Models;

using OS.Agent.Settings;

namespace OS.Agent.Webhooks;

public class InstallProcessor(IOptions<ZeroMQSettings> settings) : WebhookEventProcessor
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
            var url = @$"tcp://*:{settings.Value.Port}";
            var socket = new PublisherSocket();
            socket.Bind(url);

            var ev = new Models.Event<Installation>(
                action == InstallationAction.Created ? "github.install.create" : "github.install.delete",
                @event.Installation
            );

            socket.SendMoreFrame(ev.Name).SendFrame(ev.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return ValueTask.CompletedTask;
    }
}