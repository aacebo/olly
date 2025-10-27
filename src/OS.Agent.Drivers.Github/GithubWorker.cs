using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Drivers.Github.Events;

namespace OS.Agent.Drivers.Github;

public partial class GithubWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private ILogger<GithubWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<GithubWorker>>();
    private NetMQQueue<GithubEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<GithubEvent>>();
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
                var client = new GithubClient(@event, scope.ServiceProvider, cancellationToken);

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

    protected async Task OnEvent(GithubEvent @event, GithubClient client, CancellationToken cancellationToken = default)
    {
        if (@event is GithubInstallEvent install)
        {
            await OnInstallEvent(install, client, cancellationToken);
            return;
        }
        else if (@event is GithubMessageEvent message)
        {
            await OnMessageEvent(message, client, cancellationToken);
            return;
        }

        throw new Exception($"event '{@event.Key}' not found");
    }
}