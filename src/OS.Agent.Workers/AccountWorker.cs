using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Workers;

public class AccountWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<AccountWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<AccountWorker>>();
    private NetMQQueue<Event<AccountEvent>> Events { get; init; } = provider.GetRequiredService<NetMQQueue<Event<AccountEvent>>>();
    private JsonSerializerOptions JsonOptions { get; init; } = provider.GetRequiredService<JsonSerializerOptions>();
    private NetMQPoller Poller { get; init; } = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("starting...");
        Poller.Add(Events);
        Events.ReceiveReady += async (_, args) =>
        {
            var scope = scopeFactory.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
            var logs = scope.ServiceProvider.GetRequiredService<ILogService>();

            while (args.Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
            {
                try
                {
                    Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonOptions));

                    await logs.Create(new()
                    {
                        TenantId = @event.Body.Tenant.Id,
                        Type = LogType.Account,
                        TypeId = @event.Body.Account.Id.ToString(),
                        Text = @event.Name,
                        Entities = [Entity.From(@event.Body)]
                    }, lifetime.ApplicationStopping);
                }
                catch (Exception ex)
                {
                    Logger.LogError("{}", ex);
                    throw new Exception("AccountWorker", ex);
                }
            }
        };

        Poller.RunAsync();
        Logger.LogInformation("listening...");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("stopping...");
        Poller.StopAsync();
        Logger.LogInformation("stopped");
        return Task.CompletedTask;
    }
}