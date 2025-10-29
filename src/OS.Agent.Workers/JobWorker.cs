using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Workers;

public class JobWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<JobWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<JobWorker>>();
    private NetMQQueue<JobEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<JobEvent>>();
    private NetMQQueue<Event> TeamsQueue { get; init; } = provider.GetRequiredKeyedService<NetMQQueue<Event>>(SourceType.Teams.ToString());
    private NetMQQueue<Event> GithubQueue { get; init; } = provider.GetRequiredKeyedService<NetMQQueue<Event>>(SourceType.Github.ToString());
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

    protected async Task OnStart(NetMQQueue<JobEvent> queue, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider.GetRequiredService<IServices>();

        while (queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
        {
            Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonSerializerOptions));

            await services.Logs.Create(new()
            {
                TenantId = @event.Tenant.Id,
                Type = LogType.Job,
                TypeId = @event.Job.Id.ToString(),
                Text = @event.Key,
                Entities = [Entity.From(@event)]
            }, cancellationToken);

            try
            {
                await OnEvent(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError("{}", ex);
                await services.Logs.Create(new()
                {
                    TenantId = @event.Tenant.Id,
                    Level = Storage.Models.LogLevel.Error,
                    Type = LogType.Job,
                    TypeId = @event.Job.Id.ToString(),
                    Text = ex.Message,
                    Entities = [Entity.From(@event)]
                }, cancellationToken);
            }
        }
    }

    protected Task OnEvent(JobEvent @event, CancellationToken _ = default)
    {
        if (@event.Job.Name.StartsWith($"{SourceType.Teams}."))
        {
            TeamsQueue.Enqueue(@event);
            return Task.CompletedTask;
        }
        else if (@event.Job.Name.StartsWith($"{SourceType.Github}."))
        {
            GithubQueue.Enqueue(@event);
            return Task.CompletedTask;
        }

        throw new Exception($"job '{@event.Job.Name}' not found");
    }
}