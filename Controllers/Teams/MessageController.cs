using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using NetMQ;

using OS.Agent.Models;

namespace OS.Agent.Controllers.Teams;

[TeamsController]
public class MessageController(NetMQQueue<Event<MessageActivity>> events)
{
    [Message]
    public void OnMessage([Context] MessageActivity activity)
    {
        events.Enqueue(new Event<MessageActivity>(
            "activity.message",
            activity
        ));
    }
}