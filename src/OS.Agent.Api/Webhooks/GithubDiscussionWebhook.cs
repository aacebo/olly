using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Discussion;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Errors;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Api.Webhooks;

public class GithubDiscussionWebhook(IServiceScopeFactory scopeFactory) : WebhookEventProcessor
{
    protected override async ValueTask ProcessDiscussionWebhookAsync
    (
        WebhookHeaders headers,
        DiscussionEvent @event,
        DiscussionAction action,
        CancellationToken cancellationToken = default
    )
    {
        var installLite = @event.Installation ?? throw HttpException.UnAuthorized().AddMessage("installation not found");
        var scope = scopeFactory.CreateScope();
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var chats = scope.ServiceProvider.GetRequiredService<IChatService>();
        var messages = scope.ServiceProvider.GetRequiredService<IMessageService>();
        var tenant = await tenants.GetBySourceId(SourceType.Github, installLite.Id.ToString(), cancellationToken)
            ?? throw HttpException.UnAuthorized().AddMessage("tenant not found");

        var account = await accounts.GetBySourceId(tenant.Id, SourceType.Github, @event.Discussion.User.NodeId, cancellationToken);

        if (account is null)
        {
            account = await accounts.Create(new()
            {
                TenantId = tenant.Id,
                SourceId = @event.Discussion.User.NodeId,
                SourceType = SourceType.Github,
                Name = @event.Discussion.User.Login,
                Data = new GithubAccountData()
                {
                    User = @event.Discussion.User
                }
            }, cancellationToken);
        }
        else
        {
            if (account.Data is GithubAccountData)
            {
                account.Data = new GithubAccountData()
                {
                    User = @event.Discussion.User
                };

                account = await accounts.Update(account, cancellationToken);
            }
        }

        var chat = await chats.GetBySourceId(tenant.Id, SourceType.Github, @event.Discussion.NodeId, cancellationToken);

        if (chat is null)
        {
            chat = await chats.Create(new()
            {
                TenantId = tenant.Id,
                SourceId = @event.Discussion.NodeId,
                SourceType = SourceType.Github,
                Type = "discussion",
                Name = @event.Discussion.Title,
                Data = new GithubDiscussionData()
                {
                    Discussion = @event.Discussion
                }
            }, cancellationToken);
        }
        else
        {
            chat.Name = @event.Discussion.Title;
            chat.Type = "discussion";
            chat.Data = new GithubDiscussionData()
            {
                Discussion = @event.Discussion
            };

            chat = await chats.Update(chat, cancellationToken);
        }

        var message = await messages.GetBySourceId(chat.Id, SourceType.Github, @event.Discussion.NodeId, cancellationToken);

        if (message is null)
        {
            await messages.Create(new()
            {
                AccountId = account.Id,
                ChatId = chat.Id,
                SourceId = @event.Discussion.NodeId,
                SourceType = SourceType.Github,
                Text = @event.Discussion.Body
            }, cancellationToken);
        }
        else
        {
            message.Text = @event.Discussion.Body;
            await messages.Update(message, cancellationToken);
        }
    }
}