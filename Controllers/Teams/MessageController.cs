using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace OS.Agent.Controllers.Teams;

[TeamsController]
public class MessageController
{
    [Message]
    public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client)
    {
        await client.Send($"you said \"{activity.Text}\"");
    }
}