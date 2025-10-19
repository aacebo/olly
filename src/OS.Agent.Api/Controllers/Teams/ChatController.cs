using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using OS.Agent.Drivers.Teams.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Api.Controllers.Teams;

[TeamsController]
public class ChatController(IServiceScopeFactory scopeFactory)
{
    [Conversation.Update]
    public async Task OnUpdate(IContext<ConversationUpdateActivity> context)
    {
        var scope = scopeFactory.CreateScope();
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
            return;
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
            chat.Entities.Put(new TeamsChatEntity()
            {
                Conversation = context.Activity.Conversation,
                ServiceUrl = context.Activity.ServiceUrl
            });

            await chats.Update(chat, context.CancellationToken);
        }

        foreach (var member in context.Activity.MembersAdded)
        {
            var account = await accounts.GetBySourceId(
                tenant.Id,
                SourceType.Teams,
                member.Id,
                context.CancellationToken
            );

            if (account is null)
            {
                await accounts.Create(new()
                {
                    TenantId = tenant.Id,
                    SourceId = member.Id,
                    SourceType = SourceType.Teams,
                    Name = member.Name,
                    Entities = [
                        new TeamsAccountEntity()
                        {
                            User = member
                        }
                    ]
                }, context.CancellationToken);
            }
            else
            {
                account.Name = member.Name ?? account.Name;
                account.Entities.Put(new TeamsAccountEntity()
                {
                    User = member
                });

                await accounts.Update(account, context.CancellationToken);
            }
        }
    }
}