using System.Text.Json;

using NetMQ;

using OS.Agent.Drivers.Github.Events;
using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Workers;

public class AccountWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private ILogger<AccountWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<AccountWorker>>();
    private NetMQQueue<AccountEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<AccountEvent>>();
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
                Type = LogType.Account,
                TypeId = @event.Account.Id.ToString(),
                Text = @event.Key,
                Entities = [Entity.From(@event)]
            }, cancellationToken);

            var _ = OnEvent(@event, cancellationToken);
        }

        Logger.LogInformation("stopping...");
    }

    protected async Task OnEvent(AccountEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.Action.IsCreate)
        {
            await OnCreateEvent(@event, cancellationToken);
        }
        else if (@event.Action.IsUpdate)
        {
            await OnUpdateEvent(@event, cancellationToken);
        }
        else if (@event.Action.IsDelete)
        {
            await OnDeleteEvent(@event, cancellationToken);
        }

        throw new Exception($"event '{@event.Key}' not found");
    }

    protected Task OnCreateEvent(AccountEvent @event, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnUpdateEvent(AccountEvent @event, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnDeleteEvent(AccountEvent @event, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }
}