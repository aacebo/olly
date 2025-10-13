using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Installation;

using OS.Agent.Models;
using OS.Agent.Services;

namespace OS.Agent.Webhooks;

public class GithubInstallProcessor(IServiceScopeFactory scopeFactory) : WebhookEventProcessor
{
    protected override async ValueTask ProcessInstallationWebhookAsync
    (
        WebhookHeaders headers,
        InstallationEvent @event,
        InstallationAction action,
        CancellationToken cancellationToken = default
    )
    {
        var scope = scopeFactory.CreateScope();
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var chats = scope.ServiceProvider.GetRequiredService<IChatService>();
        var org = @event.Installation;
        var tenant = await tenants.GetBySourceId(SourceType.Github, @event.Installation.Id.ToString(), cancellationToken)
            ?? throw new UnauthorizedAccessException("tenant not found");

        var account = await accounts.GetBySourceId(tenant.Id, SourceType.Github, @event.Installation.Account.NodeId, cancellationToken);

        if (account is null)
        {
            account = await accounts.Create(new()
            {
                TenantId = tenant.Id,
                SourceType = SourceType.Github,
                SourceId = @event.Installation.Account.NodeId,
                Name = @event.Installation.Account.Login,
                Data = new Data.Account.Github()
                {
                    Install = @event.Installation,
                    User = @event.Installation.Account
                }
            }, cancellationToken);
        }
        else
        {
            account.Name = @event.Installation.Account.Login;
            account.Data = new Data.Account.Github()
            {
                Install = @event.Installation,
                User = @event.Installation.Account
            };

            account = await accounts.Update(account, cancellationToken);
        }

        if (!tenant.Sources.Any(t => t.Type == SourceType.Github && t.Id == @event.Installation.Id.ToString()))
        {
            tenant.Sources.Add(new()
            {
                Type = SourceType.Github,
                Id = @event.Installation.Id.ToString()
            });

            await tenants.Update(tenant, cancellationToken);
        }
    }
}