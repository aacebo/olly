using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Drivers;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Workers;

public class InstallWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<InstallWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<InstallWorker>>();
    private NetMQQueue<Event<InstallEvent>> Events { get; init; } = provider.GetRequiredService<NetMQQueue<Event<InstallEvent>>>();
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
                    var driver = scope.ServiceProvider.GetServices<IDriver>().FirstOrDefault(driver => driver.Type == @event.Body.Install.SourceType);

                    if (driver is null)
                    {
                        throw new NotImplementedException($"no driver implemented for source type '{@event.Body.Install.SourceType}'");
                    }

                    await logs.Create(new()
                    {
                        TenantId = @event.Body.Tenant.Id,
                        Type = LogType.Install,
                        TypeId = @event.Body.Install.Id.ToString(),
                        Text = @event.Name,
                        Entities = [Entity.From(@event.Body)]
                    }, lifetime.ApplicationStopping);

                    if (@event.Name == "installs.create")
                    {
                        await OnCreateEvent(@event, driver, lifetime.ApplicationStopping);
                    }
                    else if (@event.Name == "installs.update")
                    {
                        await OnUpdateEvent(@event, driver, lifetime.ApplicationStopping);
                    }
                    else if (@event.Name == "installs.delete")
                    {
                        await OnDeleteEvent(@event, driver, lifetime.ApplicationStopping);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("{}", ex);
                    throw new Exception("InstallWorker", ex);
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

    private async Task OnCreateEvent(Event<InstallEvent> @event, IDriver driver, CancellationToken cancellationToken = default)
    {
        await driver.Install(new()
        {
            Tenant = @event.Body.Tenant,
            Account = @event.Body.Account,
            Install = @event.Body.Install
        }, cancellationToken);
    }

    private async Task OnUpdateEvent(Event<InstallEvent> @event, IDriver driver, CancellationToken cancellationToken = default)
    {
        await driver.Install(new()
        {
            Tenant = @event.Body.Tenant,
            Account = @event.Body.Account,
            Install = @event.Body.Install
        }, cancellationToken);
    }

    private async Task OnDeleteEvent(Event<InstallEvent> @event, IDriver driver, CancellationToken cancellationToken = default)
    {
        await driver.UnInstall(new()
        {
            Tenant = @event.Body.Tenant,
            Account = @event.Body.Account,
            Install = @event.Body.Install
        }, cancellationToken);
    }
}