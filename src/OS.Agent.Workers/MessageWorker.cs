using System.Text.Json;

using NetMQ;

using OS.Agent.Drivers.Github.Events;
using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Workers;

public class MessageWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private ILogger<MessageWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<MessageWorker>>();
    private NetMQQueue<MessageEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<MessageEvent>>();
    private NetMQQueue<TeamsEvent> TeamsQueue { get; init; } = provider.GetRequiredService<NetMQQueue<TeamsEvent>>();
    private NetMQQueue<GithubEvent> GithubQueue { get; init; } = provider.GetRequiredService<NetMQQueue<GithubEvent>>();
    private JsonSerializerOptions JsonSerializerOptions { get; init; } = provider.GetRequiredService<JsonSerializerOptions>();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("starting...");

        var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider.GetRequiredService<IServices>();

        while (Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
        {
            Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));

            await services.Logs.Create(new()
            {
                TenantId = @event.Tenant.Id,
                Type = LogType.Message,
                TypeId = @event.Message.Id.ToString(),
                Text = @event.Key,
                Entities = [Entity.From(@event)]
            }, cancellationToken);

            var _ = OnEvent(@event, scope, cancellationToken);
        }

        Logger.LogInformation("stopping...");
    }

    protected async Task OnEvent(MessageEvent @event, IServiceScope scope, CancellationToken _ = default)
    {
        if (@event.Message.SourceType.IsTeams)
        {
            TeamsQueue.Enqueue(TeamsMessageEvent.From(@event));
        }
        else if (@event.Message.SourceType.IsGithub)
        {
            GithubQueue.Enqueue(GithubMessageEvent.From(@event, scope));
        }

        throw new Exception($"event source '{@event.Message.SourceType}' not found");
    }
}