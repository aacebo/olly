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
public class InstallController(JsonSerializerOptions options, IStorage storage)
{
    [Install]
    public async Task OnInstall([Context] InstallUpdateActivity activity, [Context] IContext.Client client, [Context] CancellationToken cancellationToken)
    {
        var tenantId = activity.Conversation.TenantId;
        var chatId = activity.Conversation.Id;

        if (tenantId is null)
        {
            await client.Send("⚠️Install failed due to missing data⚠️");
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

        // await client.Send(
        //     new MessageActivity()
        //     {
        //         InputHint = InputHint.AcceptingInput,
        //         Conversation = activity.Conversation
        //     }.AddAttachment(
        //         new AdaptiveCard(
        //             new Image("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR4ExGUTEwAQn95uM4KUU-OZ7Zz1n2lDrnXfw&s")
        //                 .WithHorizontalAlignment(HorizontalAlignment.Center)
        //                 .WithStyle(ImageStyle.RoundedCorners)
        //                 .WithSize(Size.Large),
        //             new ActionSet(
        //                 new SignInAction("<url>")
        //                     .WithTitle("Login")
        //                     .WithStyle(ActionStyle.Positive)
        //                     .WithIconUrl("icon:ShieldLock")
        //             ).WithHorizontalAlignment(HorizontalAlignment.Center)
        //         )
        //     )
        // );
        await client.Send("Hello! Is there anything I can help you with?");
    }
}