using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace OS.Agent.Controllers.Teams;

[TeamsController]
public class InstallController(ILogger<InstallController> logger)
{
    [Install]
    public async Task OnInstall([Context] InstallUpdateActivity activity, [Context] IContext.Client client)
    {
        logger.LogInformation("{}", activity.Type);
        await client.Send("Hola!👋");
    }
}