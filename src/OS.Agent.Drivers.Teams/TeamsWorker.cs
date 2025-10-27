using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Drivers.Teams.Events;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private ILogger<TeamsWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<TeamsWorker>>();
    private NetMQQueue<TeamsEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<TeamsEvent>>();
    private JsonSerializerOptions JsonSerializerOptions { get; init; } = provider.GetRequiredService<JsonSerializerOptions>();

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            Logger.LogInformation("starting...");

            var scope = scopeFactory.CreateScope();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200))) continue;
                Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));
                var client = new TeamsClient(@event, scope.ServiceProvider, cancellationToken);

                try
                {
                    await OnEvent(@event, client, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError("{}", ex);
                }
            }

            Logger.LogInformation("stopping...");
        }, cancellationToken);
    }

    protected async Task OnEvent(TeamsEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
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