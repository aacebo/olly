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

public class MessageWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<MessageWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<MessageWorker>>();
    private NetMQQueue<MessageEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<MessageEvent>>();
    private NetMQQueue<TeamsEvent> TeamsQueue { get; init; } = provider.GetRequiredService<NetMQQueue<TeamsEvent>>();
    private NetMQQueue<GithubEvent> GithubQueue { get; init; } = provider.GetRequiredService<NetMQQueue<GithubEvent>>();
    private JsonSerializerOptions JsonSerializerOptions { get; init; } = provider.GetRequiredService<JsonSerializerOptions>();
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

    protected async Task OnStart(NetMQQueue<MessageEvent> queue, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider.GetRequiredService<IServices>();

        while (queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
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

            try
            {
                if (@event.Message.AccountId is null) continue;
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