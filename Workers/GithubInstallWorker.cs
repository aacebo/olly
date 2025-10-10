using System.Text.Json;

using Json.More;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Models;
using OS.Agent.Stores;

namespace OS.Agent.Workers;

public class GithubInstallWorker(ILogger<GithubInstallWorker> logger, NetMQQueue<Event<GithubInstallEvent>> events, IServiceScopeFactory scopeFactory) : IHostedService
{
    private readonly NetMQPoller _poller = [events];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("starting...");
        events.ReceiveReady += async (_, args) =>
        {
            var scope = scopeFactory.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();

            while (args.Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
            {
                try
                {
                    var ok = await OnEvent(@event, storage, lifetime.ApplicationStopping);

                    if (!ok)
                    {
                        logger.LogWarning("invalid event");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("{}", ex);
                    throw new Exception("GithubInstallWorker", ex);
                }
            }
        };

        _poller.RunAsync();
        logger.LogInformation("listening...");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("stopping...");
        _poller.StopAsync();
        logger.LogInformation("stopped");
        return Task.CompletedTask;
    }

    private async Task<bool> OnEvent(Event<GithubInstallEvent> @event, IStorage storage, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("{}", @event);

        return @event.Name switch
        {
            "github.install.create" => await OnCreateEvent(@event, storage, cancellationToken),
            "github.install.delete" => await OnDeleteEvent(@event, storage, cancellationToken),
            _ => throw new Exception($"invalid event type '{@event.Name}'")
        };
    }

    private async Task<bool> OnCreateEvent(Event<GithubInstallEvent> @event, IStorage storage, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEventTenant(@event, storage, cancellationToken);
        var account = await storage.Accounts.GetBySourceId
        (
            tenant.Id,
            SourceType.Github,
            @event.Body.Install.Account.NodeId,
            cancellationToken
        );

        if (account is null)
        {
            var user = new User()
            {
                Name = @event.Body.Install.Account.Name ?? @event.Body.Install.Account.Login
            };

            account = new()
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                SourceId = @event.Body.Install.Account.NodeId,
                SourceType = SourceType.Github,
                Name = @event.Body.Install.Account.Login,
                Data = @event.Body.Install.Account.ToJsonDocument()
            };

            await storage.Users.Create(user);
            await storage.Accounts.Create(account, cancellationToken: cancellationToken);
            return true;
        }

        account.Data = @event.Body.Install.Account.ToJsonDocument();
        await storage.Accounts.Update(account, cancellationToken: cancellationToken);
        return true;
    }

    private async Task<bool> OnDeleteEvent(Event<GithubInstallEvent> @event, IStorage storage, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEventTenant(@event, storage, cancellationToken);
        var account = await storage.Accounts.GetBySourceId
        (
            tenant.Id,
            SourceType.Github,
            @event.Body.Install.Account.NodeId,
            cancellationToken
        );

        if (account is null) return false;

        await storage.Accounts.Delete(account.Id, cancellationToken: cancellationToken);
        return true;
    }

    private async Task<Tenant> GetEventTenant(Event<GithubInstallEvent> @event, IStorage storage, CancellationToken cancellationToken = default)
    {
        Tenant? tenant;

        if (@event.Body.Org is not null)
        {
            tenant = await storage.Tenants.GetBySourceId
            (
                SourceType.Github,
                @event.Body.Org.NodeId,
                cancellationToken
            );

            if (tenant is null)
            {
                return await storage.Tenants.Create(new()
                {
                    Name = @event.Body.Org.Login,
                    SourceId = @event.Body.Org.NodeId,
                    SourceType = SourceType.Github,
                    Data = @event.Body.Org.ToJsonDocument()
                }, cancellationToken: cancellationToken);
            }

            tenant.Data = @event.Body.Org.ToJsonDocument();
            return await storage.Tenants.Update(tenant, cancellationToken: cancellationToken);
        }

        tenant = await storage.Tenants.GetBySourceId
        (
            SourceType.Github,
            @event.Body.Install.Account.NodeId,
            cancellationToken
        );

        if (tenant is null)
        {
            tenant = await storage.Tenants.Create(new()
            {
                Name = @event.Body.Install.Account.Login,
                SourceId = @event.Body.Install.Account.NodeId,
                SourceType = SourceType.Github,
                Data = @event.Body.Install.Account.ToJsonDocument()
            }, cancellationToken: cancellationToken);
        }
        else
        {
            logger.LogDebug("{}", JsonSerializer.Serialize(tenant));
            tenant.Data = @event.Body.Install.Account.ToJsonDocument();
            tenant = await storage.Tenants.Update(tenant, cancellationToken: cancellationToken);
        }

        return tenant;
    }
}