using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Drivers.Teams.Events;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<TeamsWorker> Logger { get; } = provider.GetRequiredService<ILogger<TeamsWorker>>();
    private NetMQQueue<TeamsEvent> Queue { get; } = provider.GetRequiredService<NetMQQueue<TeamsEvent>>();
    private JsonSerializerOptions JsonSerializerOptions { get; } = provider.GetRequiredService<JsonSerializerOptions>();
    private IHostApplicationLifetime Lifetime { get; } = provider.GetRequiredService<IHostApplicationLifetime>();
    private NetMQPoller Poller { get; } = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("starting...");
        Poller.Add(Queue);
        Queue.ReceiveReady += async (_, args) => await OnStart(args.Queue, Lifetime.ApplicationStopping);
        Poller.RunAsync();
        Logger.LogInformation("started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("stopping...");
        Poller.StopAsync();
        Logger.LogInformation("stopped");
        return Task.CompletedTask;
    }

    protected async Task OnStart(NetMQQueue<TeamsEvent> queue, CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateScope();

        while (queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
        {
            Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));

            try
            {
                var client = ClientRegistry.Get(@event.SourceType)(@event, scope.ServiceProvider, cancellationToken);
                await OnEvent(@event, client, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError("{}", ex);
            }
        }
    }

    protected async Task OnEvent(TeamsEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        if (@event is TeamsInstallEvent install)
        {
            await OnInstallEvent(install, client, cancellationToken);
            return;
        }
        else if (@event is TeamsMessageEvent message)
        {
            await OnMessageEvent(message, client, cancellationToken);
            return;
        }

        throw new Exception($"event '{@event.Key}' not found");
    }
}