using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public class TeamsDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Teams;

    private App Teams { get; init; } = provider.GetRequiredService<App>();

    public async Task<IActivity> Send(IActivity activity, CancellationToken cancellationToken = default)
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