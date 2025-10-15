using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Controllers.Teams;

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

        var tenant = await tenants.GetBySourceId(SourceType.Teams, tenantId, context.CancellationToken) ?? throw new UnauthorizedAccessException("tenant not found");
        var account = await accounts.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.From.Id, context.CancellationToken) ?? throw new UnauthorizedAccessException("account not found");
        var chat = await chats.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.Conversation.Id, context.CancellationToken) ?? throw new UnauthorizedAccessException("chat not found");

        if (account.UserId is null)
        {
            await context.Send("Please install the Teams App to continue...");
            return;
        }

        await messages.Create(new()
        {
            AccountId = account.Id,
            ChatId = chat.Id,
            SourceId = context.Activity.Id,
            SourceType = SourceType.Teams,
            Text = context.Activity.Text,
            Data = new TeamsMessageData()
            {
                Activity = context.Activity
            }
        }, context.CancellationToken);
    }
}