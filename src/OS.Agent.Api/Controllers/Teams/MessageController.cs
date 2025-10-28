using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using OS.Agent.Drivers.Teams.Models;
using OS.Agent.Errors;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Api.Controllers.Teams;

[TeamsController]
public class MessageController(IServiceScopeFactory scopeFactory)
{
    [Message]
    public async Task OnMessage(IContext<MessageActivity> context)
    {
        var scope = scopeFactory.CreateScope();
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var chats = scope.ServiceProvider.GetRequiredService<IChatService>();
        var messages = scope.ServiceProvider.GetRequiredService<IMessageService>();
        var tenantId = context.Activity.Conversation.TenantId ?? context.TenantId;

        var tenant = await tenants.GetBySourceId(SourceType.Teams, tenantId, context.CancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("tenant not found");
        var account = await accounts.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.From.Id, context.CancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("account not found");
        var chat = await chats.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.Conversation.Id, context.CancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("chat not found");

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
            var replyTo = await messages.GetBySourceId(chat.Id, SourceType.Teams, context.Activity.ReplyToId, context.CancellationToken);
            message.ReplyToId = replyTo?.Id;
        }

        await messages.Create(message, context.CancellationToken);
    }
}