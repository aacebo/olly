using NetMQ;
using NetMQ.Sockets;

using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Installation;
using Octokit.Webhooks.Models;

namespace OS.Agent.Webhooks;

public class InstallProcessor : WebhookEventProcessor
{
    private readonly PublisherSocket _socket;

    public InstallProcessor(IConfiguration configuration)
    {
        var url = configuration.GetConnectionString("ZeroMQ") ?? throw new Exception("ConnectionStrings.ZeroMQ not found");
        _socket = new(url);
    }

    protected override ValueTask ProcessInstallationWebhookAsync
    (
        WebhookHeaders headers,
        InstallationEvent @event,
        InstallationAction action,
        CancellationToken cancellationToken = default
    )
    {
        var ev = new Models.Event<Installation>(action == InstallationAction.Created ? "github.install.create" : "github.install.delete")
        {
            Body = @event.Installation,
        };

        _socket.SendMoreFrame(ev.Name).SendFrame(ev.ToString());
        return ValueTask.CompletedTask;
    }
}