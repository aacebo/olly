using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public class TeamsDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Teams;

    private App Teams { get; init; } = provider.GetRequiredService<App>();

    public async Task<TActivity> Send<TActivity>(Account account, TActivity activity, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        return await Teams.Send(
            activity.Conversation.Id,
            activity,
            activity.Conversation.Type,
            activity.ServiceUrl,
            cancellationToken
        );
    }

    public async Task<MessageActivity> Reply(Account account, MessageActivity replyTo, MessageActivity message, CancellationToken cancellationToken = default)
    {
        message.Text = string.Join("\n", [
            replyTo.ToQuoteReply(),
            message.Text != string.Empty ? $"<p>{message.Text}</p>" : string.Empty
        ]);

        return await Teams.Send(
            message.Conversation.Id,
            message,
            message.Conversation.Type,
            message.ServiceUrl,
            cancellationToken
        );
    }
}