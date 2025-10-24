using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NetMQ;

using OS.Agent.Contexts;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage;
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
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
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
                        Type = LogType.Install,
                        TypeId = @event.Body.Install.Id.ToString(),
                        Text = @event.Name,
                        Entities = [Entity.From(@event.Body)]
                    }, lifetime.ApplicationStopping);

                    if (@event.Body.Account.UserId is null) continue;

                    var user = await storage.Users.GetById(@event.Body.Account.UserId.Value, lifetime.ApplicationStopping);

                    if (user is null) continue;

                    var context = new AgentInstallContext(@event.Body.Account.SourceType, scope, lifetime.ApplicationStopping)
                    {
                        Tenant = @event.Body.Tenant,
                        Account = @event.Body.Account,
                        User = user,
                        Installation = @event.Body.Install,
                        Chat = @event.Body.Chat,
                        Message = @event.Body.Message
                    };

                    if (@event.Name == "installs.create")
                    {
                        await OnCreateEvent(context);
                    }
                    else if (@event.Name == "installs.update")
                    {
                        await OnUpdateEvent(context);
                    }
                    else if (@event.Name == "installs.delete")
                    {
                        await OnDeleteEvent(context);
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

    private async Task OnCreateEvent(AgentInstallContext context)
    {
        await context.Install();
    }

    private async Task OnUpdateEvent(AgentInstallContext context)
    {
        await context.Install();
    }

    private async Task OnDeleteEvent(AgentInstallContext context)
    {
        await context.UnInstall();
    }
}