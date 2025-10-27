using System.Text.Json;

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
        Logger.LogInformation("starting...");

        var scope = scopeFactory.CreateScope();

        while (Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
        {
            Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));
            var client = new TeamsClient(@event, scope.ServiceProvider, cancellationToken);
            var _ = OnEvent(@event, client, cancellationToken);
        }

        Logger.LogInformation("stopping...");
        return Task.CompletedTask;
    }

    protected async Task OnEvent(TeamsEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        if (@event is TeamsInstallEvent install)
        {
            await OnInstallEvent(install, client, cancellationToken);
        }
        else if (@event is TeamsMessageEvent message)
        {
            await OnMessageEvent(message, client, cancellationToken);
        }

        throw new Exception($"event '{@event.Key}' not found");
    }
}