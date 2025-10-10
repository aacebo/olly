using System.Text.Json;

using Json.More;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using OS.Agent.Models;
using OS.Agent.Stores;

namespace OS.Agent.Controllers.Teams;

[TeamsController]
public class ChatController(JsonSerializerOptions options, IStorage storage)
{
    [Conversation.Update]
    public async Task OnUpdate([Context] ConversationUpdateActivity activity, [Context] IContext.Client client, [Context] CancellationToken cancellationToken)
    {
        var tenantId = activity.Conversation.TenantId;
        var chatId = activity.Conversation.Id;

        if (tenantId is null)
        {
            await client.Send("⚠️Conversation Update failed due to missing data⚠️");
            return;
        }

        var tenant = await storage.Tenants.GetBySourceId
        (
            SourceType.Teams,
            tenantId,
            cancellationToken
        );

        if (tenant is null)
        {
            tenant = await storage.Tenants.Create(new()
            {
                SourceId = tenantId,
                SourceType = SourceType.Teams,
                Data = activity.ChannelData!.Tenant.ToJsonDocument(options)
            }, cancellationToken: cancellationToken);
        }
        else
        {
            tenant.Data = activity.ChannelData!.Tenant.ToJsonDocument(options);
            tenant = await storage.Tenants.Update(tenant, cancellationToken: cancellationToken);
        }

        var chat = await storage.Chats.GetBySourceId
        (
            tenant.Id,
            SourceType.Teams,
            chatId,
            cancellationToken
        );

        if (chat is null)
        {
            await storage.Chats.Create(new()
            {
                TenantId = tenant.Id,
                SourceId = chatId,
                SourceType = SourceType.Teams,
                Name = activity.Conversation.Name,
                Data = activity.ChannelData!.Settings!.SelectedChannel.ToJsonDocument(options)
            }, cancellationToken: cancellationToken);
        }
        else
        {
            chat.Name = activity.Conversation.Name;
            chat.Data = activity.ChannelData!.Settings!.SelectedChannel.ToJsonDocument(options);
            await storage.Chats.Update(chat, cancellationToken: cancellationToken);
        }
    }
}