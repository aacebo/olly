using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace OS.Agent;

[TeamsController("main")]
public class MainController(ILogger<MainController> logger)
{
    [Message]
    public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client)
    {
        await client.Send($"you said \"{activity.Text}\"");
    }

    [Install]
    public async Task OnInstall([Context] InstallUpdateActivity activity, [Context] IContext.Client client)
    {
        logger.LogInformation("{}", activity.Type);
        await client.Send("Hola!👋");
    }
}