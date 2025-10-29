using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using OS.Agent.Drivers.Teams.Models;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Api.Controllers.Teams;

[TeamsController]
public class InstallController(IHttpContextAccessor accessor)
{
    [Install]
    public async Task OnInstall(IContext<InstallUpdateActivity> context)
    {
        var users = accessor.HttpContext!.RequestServices.GetRequiredService<IUserService>();
        var tenants = accessor.HttpContext!.RequestServices.GetRequiredService<ITenantService>();
        var accounts = accessor.HttpContext!.RequestServices.GetRequiredService<IAccountService>();
        var chats = accessor.HttpContext!.RequestServices.GetRequiredService<IChatService>();
        var installs = accessor.HttpContext!.RequestServices.GetRequiredService<IInstallService>();
        var messages = accessor.HttpContext!.RequestServices.GetRequiredService<IMessageStorage>();
        var tenantId = context.Activity.Conversation.TenantId ?? context.TenantId;
        var tenant = await tenants.GetBySourceId(
            SourceType.Teams,
            tenantId,
            context.CancellationToken
        );

        if (tenant is null)
        {
            tenant = await tenants.Create(new()
            {
                Sources = [Source.Teams(tenantId, context.Activity.ServiceUrl)]
            }, context.CancellationToken);
        }

        var account = await accounts.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.From.Id, context.CancellationToken);

        if (account is null)
        {
            account = await accounts.Create(new()
            {
                TenantId = tenant.Id,
                Name = context.Activity.From.Name,
                SourceId = context.Activity.From.Id,
                SourceType = SourceType.Teams,
                Entities = [
                    new TeamsAccountEntity()
                    {
                        User = context.Activity.From
                    }
                ]
            }, context.CancellationToken);
        }
        else
        {
            account.Name = context.Activity.From.Name;
            account.Entities.Put(new TeamsAccountEntity()
            {
                User = context.Activity.From
            });

            account = await accounts.Update(account, context.CancellationToken);
        }

        var chat = await chats.GetBySourceId(
            tenant.Id,
            SourceType.Teams,
            context.Activity.Conversation.Id,
            context.CancellationToken
        );

        if (chat is null)
        {
            chat = await chats.Create(new()
            {
                TenantId = tenant.Id,
                SourceId = context.Activity.Conversation.Id,
                SourceType = SourceType.Teams,
                Url = context.Activity.ServiceUrl,
                Type = context.Activity.Conversation.Type?.ToString(),
                Name = context.Activity.Conversation.Name,
                Entities = [
                    new TeamsChatEntity()
                    {
                        Conversation = context.Activity.Conversation,
                        ServiceUrl = context.Activity.ServiceUrl
                    }
                ]
            }, context.CancellationToken);
        }
        else
        {
            chat.Name = context.Activity.Conversation.Name;
            chat.Type = context.Activity.Conversation.Type?.ToString();
            chat.Url = context.Activity.ServiceUrl;
            chat.Entities.Put(new TeamsChatEntity()
            {
                Conversation = context.Activity.Conversation,
                ServiceUrl = context.Activity.ServiceUrl
            });

            chat = await chats.Update(chat, context.CancellationToken);
        }

        var install = await installs.GetBySourceId(
            SourceType.Teams,
            account.SourceId,
            context.CancellationToken
        );

        if (install is null)
        {
            var activity = await context.Send("Hello! Is there anything I can help you with?");
            var message = await messages.Create(new()
            {
                ChatId = chat.Id,
                SourceType = SourceType.Teams,
                SourceId = activity.Id,
                Text = activity.Text,
                Url = $"{context.Activity.ServiceUrl}v3/conversations/{context.Activity.Conversation.Id}/activities/{activity.Id}",
                Entities = [
                    new TeamsMessageEntity()
                    {
                        Activity = activity
                    }
                ]
            }, cancellationToken: context.CancellationToken);

            var user = await users.Create(new()
            {
                Name = context.Activity.From.Name
            }, context.CancellationToken);

            await installs.Create(new()
            {
                UserId = user.Id,
                AccountId = account.Id,
                MessageId = message.Id,
                SourceType = SourceType.Teams,
                SourceId = account.SourceId,
                Url = context.Activity.ServiceUrl
            }, context.CancellationToken);
        }
        else
        {
            install.Url = context.Activity.ServiceUrl;
            await installs.Update(install, context.CancellationToken);
        }
    }
}