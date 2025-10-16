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
}