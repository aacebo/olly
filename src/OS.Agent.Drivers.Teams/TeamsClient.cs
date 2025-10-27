using Microsoft.Teams.Apps;

using OS.Agent.Cards.Extensions;
using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsClient : DriverClient, IAuthClient, IChatClient, IProgressClient, ITaskClient
{
    public TeamsEvent Event { get; }

    protected App Teams { get; }

    public TeamsClient(TeamsEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default) : base(SourceType.Teams, provider, cancellationToken)
    {
        Event = @event;
        Teams = provider.GetRequiredService<App>();
    }
    
    public async Task SignIn(string url, string state)
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