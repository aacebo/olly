using System.Text.Json;

using NetMQ;
using NetMQ.Sockets;

using Octokit.Webhooks.Models;

namespace OS.Agent.Workers;

public class InstallWorker : IHostedService
{
    private readonly string _url;
    private readonly ILogger<InstallWorker> _logger;
    private readonly SubscriberSocket _socket;

    public InstallWorker(ILogger<InstallWorker> logger, IConfiguration configuration)
    {
        _url = configuration.GetConnectionString("ZeroMQ") ?? throw new Exception("ConnectionStrings.ZeroMQ not found");
        _logger = logger;
        _socket = new();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _socket.Connect(_url);
        _socket.Subscribe("github.install.create");
        _socket.Subscribe("github.install.delete");
        Task.Run(() => OnReceive(cancellationToken), cancellationToken);
        _logger.LogInformation("connected to '{}'", _url);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _socket.Disconnect(_url);
        _logger.LogInformation("disconnected from '{}'", _url);
        return Task.CompletedTask;
    }

    public Task OnReceive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var _ = _socket.ReceiveFrameString();
            var bytes = _socket.ReceiveFrameBytes();
            var @event = JsonSerializer.Deserialize<Models.Event<Installation>>(bytes) ?? throw new Exception("invalid event payload");
            Console.WriteLine(@event);
        }

        return Task.CompletedTask;
    }
}