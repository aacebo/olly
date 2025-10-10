using Json.More;

using NetMQ;

using Octokit.Webhooks.Events;

using OS.Agent.Models;
using OS.Agent.Stores;

namespace OS.Agent.Workers;

public class GithubInstallWorker(ILogger<GithubInstallWorker> logger, NetMQQueue<Event<InstallationEvent>> events, IServiceScopeFactory scopeFactory) : IHostedService
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

    private async Task<bool> OnEvent(Event<InstallationEvent> @event, IStorage storage, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("{}", @event);

        return @event.Name switch
        {
            "github.install.create" => await OnCreateEvent(@event, storage, cancellationToken),
            "github.install.delete" => await OnDeleteEvent(@event, storage, cancellationToken),
            _ => throw new Exception($"invalid event type '{@event.Name}'")
        };
    }

    private async Task<bool> OnCreateEvent(Event<InstallationEvent> @event, IStorage storage, CancellationToken cancellationToken = default)
    {
        var org = @event.Body.Organization;

        if (org is null)
        {
            return false;
        }

        var tenant = await storage.Tenants.GetBySourceId
        (
            SourceType.Github,
            org.NodeId.ToString() ?? string.Empty,
            cancellationToken
        );

        if (tenant is null)
        {
            tenant = await storage.Tenants.Create(new()
            {
                Name = org.Login,
                SourceId = org.NodeId,
                SourceType = SourceType.Github,
                Data = org.ToJsonDocument()
            }, cancellationToken: cancellationToken);
        }
        else
        {
            tenant.Data = org.ToJsonDocument();
            tenant = await storage.Tenants.Update(tenant, cancellationToken: cancellationToken);
        }

        var account = await storage.Accounts.GetBySourceId
        (
            tenant.Id,
            SourceType.Github,
            @event.Body.Installation.Account.NodeId.ToString(),
            cancellationToken
        );

        if (account is null)
        {
            var user = new User()
            {
                Name = @event.Body.Installation.Account.Name ?? @event.Body.Installation.Account.Login
            };

            account = new()
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                SourceId = @event.Body.Installation.Account.NodeId.ToString(),
                SourceType = SourceType.Github,
                Name = @event.Body.Installation.Account.Login,
                Data = @event.Body.Installation.ToJsonDocument()
            };

            await storage.Users.Create(user);
            await storage.Accounts.Create(account, cancellationToken: cancellationToken);
            return true;
        }

        account.Data = @event.Body.Installation.ToJsonDocument();
        await storage.Accounts.Update(account, cancellationToken: cancellationToken);
        return true;
    }

    private async Task<bool> OnDeleteEvent(Event<InstallationEvent> @event, IStorage storage, CancellationToken cancellationToken = default)
    {
        var org = @event.Body.Organization;

        if (org is null)
        {
            return false;
        }

        var tenant = await storage.Tenants.GetBySourceId
        (
            SourceType.Github,
            org.NodeId.ToString() ?? string.Empty,
            cancellationToken
        );

        if (tenant is null)
        {
            return false;
        }

        var account = await storage.Accounts.GetBySourceId
        (
            tenant.Id,
            SourceType.Github,
            @event.Body.Installation.Account.NodeId.ToString(),
            cancellationToken
        );

        if (account is null) return false;

        await storage.Accounts.Delete(account.Id, cancellationToken: cancellationToken);
        return true;
    }
}