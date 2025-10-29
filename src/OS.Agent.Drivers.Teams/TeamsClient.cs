using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps;

using OS.Agent.Cards.Extensions;
using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsClient(TeamsEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default) : Client(provider, cancellationToken)
{
    public TeamsEvent Event { get; } = @event;
    public override Tenant Tenant => Event.Tenant;
    public override Account Account => Event.Account;
    public override User User => Event.CreatedBy ?? throw new NullReferenceException("created_by is null");
    public override Chat Chat => Event.Chat;
    public override Message Message => Event is TeamsMessageEvent messageEvent
        ? messageEvent.Message
        : throw new NullReferenceException("message is null");

    protected App Teams { get; } = provider.GetRequiredService<App>();

    public override async Task SignIn(string url, string state)
    {
        var chatType = Event.Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(Event.Chat.Type);

        await Teams.Send(
            Event.Chat.SourceId,
            new Microsoft.Teams.Api.Activities.MessageActivity()
            {
                InputHint = Microsoft.Teams.Api.InputHint.AcceptingInput,
                Conversation = new()
                {
                    Id = Event.Chat.SourceId,
                    Type = chatType,
                    Name = Event.Chat.Name
                }
            }.AddAttachment(
                Cards.Authentication.SignInCard.Github($"{url}&state={state}")
                    .Render()
                    .ToAdaptiveCard()
            ),
            chatType,
            Event.Chat.Url,
            CancellationToken
        );
    }
}