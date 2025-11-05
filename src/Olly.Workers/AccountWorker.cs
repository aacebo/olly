using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using Olly.Events;
using Olly.Services;
using Olly.Storage.Models;

namespace Olly.Workers;

public class AccountWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<AccountWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<AccountWorker>>();
    private NetMQQueue<AccountEvent> Queue { get; init; } = provider.GetRequiredService<NetMQQueue<AccountEvent>>();
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

    protected async Task OnStart(NetMQQueue<AccountEvent> queue, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider.GetRequiredService<IServices>();

        while (queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
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
                    Type = LogType.Account,
                    TypeId = @event.Account.Id.ToString(),
                    Text = ex.Message,
                    Entities = [Entity.From(@event)]
                }, cancellationToken);
            }
        }
    }

    protected Task OnEvent(AccountEvent @event, CancellationToken _ = default)
    {
        if (@event.Account.SourceType.IsTeams)
        {
            TeamsQueue.Enqueue(@event);
        }
        else if (@event.Account.SourceType.IsGithub)
        {
            GithubQueue.Enqueue(@event);
        }

        return Task.CompletedTask;
    }
}