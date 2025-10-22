using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Installation;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Errors;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Api.Webhooks;

public class GithubInstallWebhook(IServiceScopeFactory scopeFactory) : WebhookEventProcessor
{
    protected override async ValueTask ProcessInstallationWebhookAsync
    (
        WebhookHeaders headers,
        InstallationEvent @event,
        InstallationAction action,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(1000, cancellationToken);

        var scope = scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<GitHubClient>();
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var chats = scope.ServiceProvider.GetRequiredService<IChatService>();
        var tenant = await tenants.GetBySourceId(SourceType.Github, @event.Installation.Id.ToString(), cancellationToken)
            ?? throw HttpException.UnAuthorized().AddMessage("tenant not found");

        var account = await accounts.GetBySourceId(tenant.Id, SourceType.Github, @event.Installation.Account.NodeId, cancellationToken);
        var install = await client.GitHubApps.GetInstallationForCurrent(@event.Installation.Id);
        var accessToken = await client.GitHubApps.CreateInstallationToken(@event.Installation.Id);

        if (account is null)
        {
            account = await accounts.Create(new()
            {
                TenantId = tenant.Id,
                SourceType = SourceType.Github,
                SourceId = @event.Installation.Account.NodeId,
                Url = @event.Installation.Account.Url,
                Name = @event.Installation.Account.Login,
                Entities = [
                    new GithubUserEntity()
                    {
                        User = new()
                        {
                            Id = install.Account.Id,
                            NodeId = install.Account.NodeId,
                            Type = install.Account.Type?.ToString(),
                            Login = install.Account.Login,
                            Name = install.Account.Name,
                            Email = install.Account.Email,
                            Url = install.Account.Url,
                            AvatarUrl = install.Account.AvatarUrl
                        }
                    },
                    new GithubInstallEntity()
                    {
                        Install = install,
                        AccessToken = accessToken
                    }
                ]
            }, cancellationToken);
        }
        else
        {
            account.Name = @event.Installation.Account.Login;
            account.Url = @event.Installation.Account.Url;
            account.Entities.Put(new GithubUserEntity()
            {
                User = new()
                {
                    Id = install.Account.Id,
                    NodeId = install.Account.NodeId,
                    Type = install.Account.Type?.ToString(),
                    Login = install.Account.Login,
                    Name = install.Account.Name,
                    Email = install.Account.Email,
                    Url = install.Account.Url,
                    AvatarUrl = install.Account.AvatarUrl
                }
            });

            account.Entities.Put(new GithubInstallEntity()
            {
                Install = install,
                AccessToken = accessToken
            });

            account = await accounts.Update(account, cancellationToken);
        }

        if (!tenant.Sources.Any(t => t.Type == SourceType.Github && t.Id == @event.Installation.Id.ToString()))
        {
            tenant.Sources.Add(new()
            {
                Type = SourceType.Github,
                Id = @event.Installation.Id.ToString(),
                Url = @event.Installation.HtmlUrl
            });

            await tenants.Update(tenant, cancellationToken);
        }
    }
}