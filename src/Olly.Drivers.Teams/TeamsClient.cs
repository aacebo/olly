using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps;

using Olly.Cards.Extensions;
using Olly.Events;
using Olly.Storage.Models;

namespace Olly.Drivers.Teams;

public partial class TeamsClient : Client
{
    public override Tenant Tenant { get; }
    public override Account Account { get; }
    public override User? User { get; }
    public override Install Install { get; }
    public override Chat Chat { get; }
    public override Message? Message { get; }

    protected Event Event { get; }
    protected App Teams { get; }

    public TeamsClient(InstallEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default) : base(provider, cancellationToken)
    {
        Event = @event;
        Tenant = @event.Tenant;
        Account = @event.Account;
        User = @event.CreatedBy;
        Install = @event.Install;
        Chat = @event.Chat;
        Message = @event.Message;
        Teams = provider.GetRequiredService<App>();
    }

    public TeamsClient(MessageEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default) : base(provider, cancellationToken)
    {
        Event = @event;
        Tenant = @event.Tenant;
        Account = @event.Account;
        User = @event.CreatedBy;
        Install = @event.Install;
        Chat = @event.Chat;
        Message = @event.Message;
        Teams = provider.GetRequiredService<App>();
    }

    public override async Task SignIn(string url, string state)
    {
        var chatType = Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(Chat.Type);

        await Teams.Send(
            Chat.SourceId,
            new Microsoft.Teams.Api.Activities.MessageActivity()
            {
                InputHint = Microsoft.Teams.Api.InputHint.AcceptingInput,
                Conversation = new()
                {
                    Id = Chat.SourceId,
                    Type = chatType,
                    Name = Chat.Name
                }
            }.AddAttachment(
                Cards.Authentication.SignInCard.Github($"{url}&state={state}")
                    .Render()
                    .ToAdaptiveCard()
            ),
            chatType,
            Chat.Url,
            CancellationToken
        );
    }
}