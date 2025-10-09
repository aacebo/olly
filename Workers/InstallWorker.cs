using System.Text.Json;

using NetMQ;

using Octokit.Webhooks.Models;

using OS.Agent.Models;
using OS.Agent.Stores;

namespace OS.Agent.Workers;

public class InstallWorker(ILogger<InstallWorker> logger, NetMQQueue<Event<Installation>> events, IServiceScopeFactory scopeFactory) : IHostedService
{
    private readonly NetMQPoller _poller = [events];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("starting...");
        events.ReceiveReady += async (_, args) =>
        {
            var scope = scopeFactory.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
            var userStorage = scope.ServiceProvider.GetRequiredService<IUserStorage>();
            var accountStorage = scope.ServiceProvider.GetRequiredService<IAccountStorage>();

            while (args.Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
            {
                try
                {
                    var ok = await OnEvent(@event, userStorage, accountStorage, lifetime.ApplicationStopping);

                    if (!ok)
                    {
                        logger.LogWarning("invalid event");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("{}", ex);
                    throw new Exception("InstallWorker", ex);
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

    private async Task<bool> OnEvent(Event<Installation> @event, IUserStorage userStorage, IAccountStorage accountStorage, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("{}", @event);

        return @event.Name switch
        {
            "github.install.create" => await OnCreateEvent(@event, userStorage, accountStorage, cancellationToken),
            "github.install.delete" => await OnDeleteEvent(@event, accountStorage, cancellationToken),
            _ => throw new Exception($"invalid event type '{@event.Name}'")
        };
    }

    private async Task<bool> OnCreateEvent(Event<Installation> @event, IUserStorage userStorage, IAccountStorage accountStorage, CancellationToken cancellationToken = default)
    {
        var account = await accountStorage.GetByExternalId
        (
            AccountType.Github, @event.Body.Account.Id.ToString(),
            cancellationToken
        );

        logger.LogDebug("account => {}", account);

        if (account is null)
        {
            var user = new Models.User()
            {
                Name = @event.Body.Account.Name ?? @event.Body.Account.Login
            };

            account = new()
            {
                UserId = user.Id,
                ExternalId = @event.Body.Account.Id.ToString(),
                Type = AccountType.Github,
                Name = @event.Body.Account.Login,
                Data = JsonSerializer.SerializeToDocument(@event.Body)
            };

            await userStorage.Create(user);
            await accountStorage.Create(account, cancellationToken: cancellationToken);
            return true;
        }

        account.Data = JsonSerializer.SerializeToDocument(@event.Body);
        await accountStorage.Update(account, cancellationToken: cancellationToken);
        return true;
    }

    private async Task<bool> OnDeleteEvent(Event<Installation> @event, IAccountStorage accountStorage, CancellationToken cancellationToken = default)
    {
        var account = await accountStorage.GetByExternalId
        (
            AccountType.Github, @event.Body.Account.Id.ToString(),
            cancellationToken
        );

        logger.LogDebug("account => {}", account);

        if (account is null) return false;

        await accountStorage.Delete(account.Id, cancellationToken: cancellationToken);
        return true;
    }
}