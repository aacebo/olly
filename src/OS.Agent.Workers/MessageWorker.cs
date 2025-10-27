using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            Logger.LogInformation("starting...");

            var scope = scopeFactory.CreateScope();
            var services = scope.ServiceProvider.GetRequiredService<IServices>();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200))) continue;
                Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));

                await services.Logs.Create(new()
                {
                    TenantId = @event.Tenant.Id,
                    Type = LogType.Message,
                    TypeId = @event.Message.Id.ToString(),
                    Text = @event.Key,
                    Entities = [Entity.From(@event)]
                }, cancellationToken);

                try
                {
                    await OnEvent(@event, scope, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError("{}", ex);
                    await services.Logs.Create(new()
                    {
                        TenantId = @event.Tenant.Id,
                        Level = Storage.Models.LogLevel.Error,
                        Type = LogType.Message,
                        TypeId = @event.Message.Id.ToString(),
                        Text = ex.Message,
                        Entities = [Entity.From(@event)]
                    }, cancellationToken);
                }
            }

            Logger.LogInformation("stopping...");
        }, cancellationToken);
    }

    protected Task OnEvent(MessageEvent @event, IServiceScope scope, CancellationToken _ = default)
    {
        if (@event.Message.SourceType.IsTeams)
        {
            TeamsQueue.Enqueue(TeamsMessageEvent.From(@event));
            return Task.CompletedTask;
        }
        else if (@event.Message.SourceType.IsGithub)
        {
            GithubQueue.Enqueue(GithubMessageEvent.From(@event, scope));
            return Task.CompletedTask;
        }

        throw new Exception($"event source '{@event.Message.SourceType}' not found");
    }
}