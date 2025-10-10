using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace OS.Agent.Controllers.Teams;

[TeamsController]
public class ChatController(ILogger<ChatController> logger)
{
    [Conversation.Update]
    public async Task OnUpdate([Context] ConversationUpdateActivity activity, [Context] IContext.Client client)
    {
        logger.LogInformation("{}", activity.Type);
        await client.Send("Hola!👋");
    }
}