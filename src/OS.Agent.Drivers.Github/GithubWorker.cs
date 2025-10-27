using System.Text.Json;

using NetMQ;

using OS.Agent.Drivers.Github.Events;

namespace OS.Agent.Drivers.Github;

public partial class GithubWorker(IServiceProvider provider) : BackgroundService
{
    private ILogger<GithubWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<GithubWorker>>();
    private NetMQQueue<GithubEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<GithubEvent>>();
    private JsonSerializerOptions JsonSerializerOptions { get; init; } = provider.GetRequiredService<JsonSerializerOptions>();

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("starting...");

        while (Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
        {
            Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));
            var _ = OnEvent(@event, cancellationToken);
        }

        Logger.LogInformation("stopping...");
        return Task.CompletedTask;
    }

    protected async Task OnEvent(GithubEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event is GithubInstallEvent install)
        {
            await OnInstallEvent(install, cancellationToken);
        }
        else if (@event is GithubMessageEvent message)
        {
            await OnMessageEvent(message, cancellationToken);
        }

        throw new Exception($"event '{@event.Key}' not found");
    }
}