using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using Olly.Drivers.Teams.Models;
using Olly.Services;
using Olly.Storage.Models;

namespace Olly.Api.Controllers.Teams;

[TeamsController]
public class ChatController(IHttpContextAccessor accessor)
{
    private IServices Services => accessor.HttpContext!.RequestServices.GetRequiredService<IServices>();

    [Conversation.Update]
    public async Task OnUpdate(IContext<ConversationUpdateActivity> context)
    {
        var tenantId = context.Activity.Conversation.TenantId ?? context.TenantId;
        var tenant = await Services.Tenants.GetBySourceId(
            SourceType.Teams,
            tenantId,
            context.CancellationToken
        );

        if (tenant is null) return;

        var chat = await Services.Chats.GetBySourceId(
            tenant.Id,
            SourceType.Teams,
            context.Activity.Conversation.Id,
            context.CancellationToken
        );

        if (chat is null)
        {
            await Services.Chats.Create(new()
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

            await Services.Chats.Update(chat, context.CancellationToken);
        }

        foreach (var member in context.Activity.MembersAdded)
        {
            var account = await Services.Accounts.GetBySourceId(
                tenant.Id,
                SourceType.Teams,
                member.Id,
                context.CancellationToken
            );

            if (account is null)
            {
                await Services.Accounts.Create(new()
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

                await Services.Accounts.Update(account, context.CancellationToken);
            }
        }
    }
}