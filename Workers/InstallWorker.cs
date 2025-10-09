using System.Text.Json;

using Microsoft.Extensions.Options;
using Microsoft.Teams.Plugins.AspNetCore.DevTools;

using NetMQ;
using NetMQ.Sockets;

using Octokit.Webhooks.Models;

using OS.Agent.Extensions;
using OS.Agent.Settings;
using OS.Agent.Stores;

namespace OS.Agent.Workers;

public class InstallWorker : BackgroundService
{
    private readonly string _url;
    private readonly ILogger<InstallWorker> _logger;
    private readonly IUserStorage _userStorage;
    private readonly IAccountStorage _accountStorage;

    public InstallWorker(ILogger<InstallWorker> logger, NetMQQueue<IEvent> events, IUserStorage userStorage, IAccountStorage accountStorage) : base()
    {
        _logger = logger;
        _userStorage = userStorage;
        _accountStorage = accountStorage;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var socket = new SubscriberSocket();
        socket.Connect(_url);
        socket.Subscribe("github.install.create");
        socket.Subscribe("github.install.delete");
        _logger.LogInformation("connected to '{}'", _url);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!socket.TryReceiveFrameString(out var key))
            {
                await Task.Delay(250, cancellationToken);
                continue;
            }

            var bytes = socket.ReceiveFrameBytes();
            var @event = JsonSerializer.Deserialize<Models.Event<Installation>>(bytes) ?? throw new Exception("invalid event payload");
            _logger.LogDebug("[{}] => {}", key, @event);

            if (@event.Name == "github.install.create")
            {
                await OnCreateEvent(@event);
            }
            else
            {
                await OnDeleteEvent(@event);
            }
        }

        socket.Disconnect(_url);
        _logger.LogInformation("disconnected from '{}'", _url);
    }

    private async Task OnCreateEvent(Models.Event<Installation> @event)
    {
        var account = await _accountStorage.GetByExternalId(Models.AccountType.Github, @event.Body.Account.Id.ToString());
        _logger.LogDebug("{}", account);

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
                Type = Models.AccountType.Github,
                Name = @event.Body.Account.Login,
            };

            await _userStorage.Create(user);
            await _accountStorage.Create(account);
            return;
        }

        account.Data = @event.Body.ToDictionary();
        await _accountStorage.Update(account);
    }

    private async Task OnDeleteEvent(Models.Event<Installation> @event)
    {
        var account = await _accountStorage.GetByExternalId(Models.AccountType.Github, @event.Body.Account.Id.ToString());
        _logger.LogDebug("{}", account);

        if (account is null)
        {
            return;
        }

        await _accountStorage.Delete(account.Id);
    }
}