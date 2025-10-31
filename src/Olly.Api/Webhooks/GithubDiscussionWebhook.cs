using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Discussion;

using Olly.Drivers.Github.Models;
using Olly.Errors;
using Olly.Services;
using Olly.Storage.Models;

namespace Olly.Api.Webhooks;

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
        await using var scope = scopeFactory.CreateAsyncScope();
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
                Url = @event.Discussion.User.HtmlUrl,
                Name = @event.Discussion.User.Login,
                Entities = [
                    new GithubUserEntity()
                    {
                        User = new()
                        {
                            Id = @event.Discussion.User.Id,
                            NodeId = @event.Discussion.User.NodeId,
                            Type = @event.Discussion.User.Type.ToString(),
                            Login = @event.Discussion.User.Login,
                            Name = @event.Discussion.User.Name,
                            Email = @event.Discussion.User.Email,
                            Url = @event.Discussion.User.HtmlUrl,
                            AvatarUrl = @event.Discussion.User.AvatarUrl
                        }
                    }
                ]
            }, cancellationToken);
        }
        else
        {
            var entity = account.Entities.Get<GithubUserEntity>();

            if (entity is not null)
            {
                entity.User = new()
                {
                    Id = @event.Discussion.User.Id,
                    NodeId = @event.Discussion.User.NodeId,
                    Type = @event.Discussion.User.Type.ToString(),
                    Login = @event.Discussion.User.Login,
                    Name = @event.Discussion.User.Name,
                    Email = @event.Discussion.User.Email,
                    Url = @event.Discussion.User.HtmlUrl,
                    AvatarUrl = @event.Discussion.User.AvatarUrl
                };

                account.Url = @event.Discussion.User.HtmlUrl;
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
                Url = @event.Discussion.HtmlUrl,
                Type = "discussion",
                Name = @event.Discussion.Title,
                Entities = [
                    new GithubDiscussionEntity()
                    {
                        Discussion = @event.Discussion
                    }
                ]
            }, cancellationToken);
        }
        else
        {
            chat.Name = @event.Discussion.Title;
            chat.Type = "discussion";
            chat.Url = @event.Discussion.HtmlUrl;
            chat.Entities.Put(new GithubDiscussionEntity()
            {
                Discussion = @event.Discussion
            });

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
                Url = @event.Discussion.HtmlUrl,
                Text = @event.Discussion.Body
            }, cancellationToken);
        }
        else
        {
            message.Text = @event.Discussion.Body;
            message.Url = @event.Discussion.HtmlUrl;
            await messages.Update(message, cancellationToken);
        }
    }
}