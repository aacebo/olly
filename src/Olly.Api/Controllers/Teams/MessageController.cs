using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using Olly.Drivers.Teams.Models;
using Olly.Errors;
using Olly.Services;
using Olly.Storage.Models;

namespace Olly.Api.Controllers.Teams;

[TeamsController]
public class MessageController(IHttpContextAccessor accessor)
{
    private IServices Services => accessor.HttpContext!.RequestServices.GetRequiredService<IServices>();

    [Message]
    public async Task OnMessage(IContext<MessageActivity> context)
    {
        var tenantId = context.Activity.Conversation.TenantId ?? context.TenantId;
        var tenant = await Services.Tenants.GetBySourceId(SourceType.Teams, tenantId, context.CancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("tenant not found");
        var account = await Services.Accounts.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.From.Id, context.CancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("account not found");
        var chat = await Services.Chats.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.Conversation.Id, context.CancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("chat not found");

        if (account.Name != context.Activity.From.Name)
        {
            account.Name = context.Activity.From.Name;
            account.Entities.Put(new TeamsAccountEntity()
            {
                User = context.Activity.From
            });

            account = await Services.Accounts.Update(account, context.CancellationToken);
        }

        var message = new Storage.Models.Message()
        {
            AccountId = account.Id,
            ChatId = chat.Id,
            SourceId = context.Activity.Id,
            SourceType = SourceType.Teams,
            Url = $"{context.Activity.ServiceUrl}v3/conversations/{context.Activity.Conversation.Id}/activities/{context.Activity.Id}",
            Text = context.Activity.Text,
            Entities = [
                new TeamsMessageEntity()
                {
                    Activity = context.Activity
                }
            ]
        };

        if (context.Activity.ReplyToId is not null)
        {
            var replyTo = await Services.Messages.GetBySourceId(chat.Id, SourceType.Teams, context.Activity.ReplyToId, context.CancellationToken);
            message.ReplyToId = replyTo?.Id;
        }

        await Services.Messages.Create(message, context.CancellationToken);
    }
}