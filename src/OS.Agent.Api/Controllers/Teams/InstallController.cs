using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using OS.Agent.Drivers.Teams.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Api.Controllers.Teams;

[TeamsController]
public class InstallController(IServiceScopeFactory scopeFactory)
{
    [Install]
    public async Task OnInstall(IContext<InstallUpdateActivity> context)
    {
        var scope = scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var chats = scope.ServiceProvider.GetRequiredService<IChatService>();
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
                Sources = [Source.Teams(tenantId)]
            }, context.CancellationToken);
        }

        var chat = await chats.GetBySourceId(
            tenant.Id,
            SourceType.Teams,
            context.Activity.Conversation.Id,
            context.CancellationToken
        );

        if (chat is null)
        {
            await chats.Create(new()
            {
                TenantId = tenant.Id,
                SourceId = context.Activity.Conversation.Id,
                SourceType = SourceType.Teams,
                Type = context.Activity.Conversation.Type,
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
            chat.Type = context.Activity.Conversation.Type;
            chat.Entities.Put(new TeamsChatEntity()
            {
                Conversation = context.Activity.Conversation,
                ServiceUrl = context.Activity.ServiceUrl
            });

            await chats.Update(chat, context.CancellationToken);
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

        if (account.UserId is null)
        {
            var user = await users.Create(new()
            {
                Name = context.Activity.From.Name
            }, context.CancellationToken);

            account.UserId = user.Id;
            await accounts.Update(account, context.CancellationToken);
        }

        await context.Send("Hello! Is there anything I can help you with?");
    }
}