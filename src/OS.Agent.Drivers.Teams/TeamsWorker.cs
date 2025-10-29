using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<TeamsWorker> Logger { get; } = provider.GetRequiredService<ILogger<TeamsWorker>>();
    private NetMQQueue<Event> Queue { get; } = provider.GetRequiredKeyedService<NetMQQueue<Event>>(SourceType.Teams.ToString());
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

    protected async Task OnStart(NetMQQueue<Event> queue, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        while (queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
        {
            Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));

            try
            {
                await OnEvent(@event, scope.ServiceProvider, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError("{}", ex);
            }
        }
    }

    protected async Task OnEvent(Event @event, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        if (@event is InstallEvent install)
        {
            var factory = ClientRegistry.Get(install.Chat?.SourceType ?? install.Install.SourceType);
            var client = factory(@event, provider, cancellationToken);
            await OnInstallEvent(install, client, cancellationToken);
            return;
        }
        else if (@event is MessageEvent message)
        {
            var factory = ClientRegistry.Get(message.Chat.SourceType);
            var client = factory(@event, provider, cancellationToken);
            await OnMessageEvent(message, client, cancellationToken);
            return;
        }

        throw new Exception($"event '{@event.Key}' not found");
    }
}