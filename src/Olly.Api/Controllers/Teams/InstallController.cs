using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using Olly.Drivers.Teams.Models;
using Olly.Services;
using Olly.Storage.Models;

namespace Olly.Api.Controllers.Teams;

[TeamsController]
public class InstallController(IHttpContextAccessor accessor)
{
    private IServices Services => accessor.HttpContext!.RequestServices.GetRequiredService<IServices>();

    [Install]
    public async Task OnInstall(IContext<InstallUpdateActivity> context)
    {
        var tenantId = context.Activity.Conversation.TenantId ?? context.TenantId;
        var tenant = await Services.Tenants.GetBySourceId(
            SourceType.Teams,
            tenantId,
            context.CancellationToken
        );

        if (tenant is null)
        {
            tenant = await Services.Tenants.Create(new()
            {
                Sources = [Source.Teams(tenantId, context.Activity.ServiceUrl)]
            }, context.CancellationToken);
        }

        var account = await Services.Accounts.GetBySourceId(tenant.Id, SourceType.Teams, context.Activity.From.Id, context.CancellationToken);

        if (account is null)
        {
            account = await Services.Accounts.Create(new()
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

            account = await Services.Accounts.Update(account, context.CancellationToken);
        }

        var chat = await Services.Chats.GetBySourceId(
            tenant.Id,
            SourceType.Teams,
            context.Activity.Conversation.Id,
            context.CancellationToken
        );

        if (chat is null)
        {
            chat = await Services.Chats.Create(new()
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

            chat = await Services.Chats.Update(chat, context.CancellationToken);
        }

        var accessToken = await context.SignIn(new OAuthOptions()
        {
            ConnectionName = "graph"
        });

        var jwt = accessToken is null ? null : new JsonWebToken(accessToken);
        var install = await Services.Installs.GetBySourceId(
            SourceType.Teams,
            account.SourceId,
            context.CancellationToken
        );

        if (install is null)
        {
            var user = await Services.Users.Create(new()
            {
                Name = context.Activity.From.Name
            }, context.CancellationToken);

            await Services.Installs.Create(new()
            {
                UserId = user.Id,
                AccountId = account.Id,
                ChatId = chat.Id,
                SourceType = SourceType.Teams,
                SourceId = account.SourceId,
                Url = context.Activity.ServiceUrl,
                AccessToken = accessToken,
                ExpiresAt = jwt?.Expiration,
                Entities = [Entity.From(context.Activity)]
            }, context.CancellationToken);
        }
        else
        {
            install.Url = context.Activity.ServiceUrl;
            install.AccessToken = accessToken;
            install.ExpiresAt = jwt?.Expiration;
            install.Entities = [Entity.From(context.Activity)];
            await Services.Installs.Update(install, context.CancellationToken);
        }
    }
}