using OS.Agent.Drivers.Teams.Events;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsWorker
{
    protected async Task OnMessageEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        if (@event.Action.IsCreate)
        {
            await OnMessageCreateEvent(@event, client, cancellationToken);
        }
        else if (@event.Action.IsUpdate)
        {
            await OnMessageUpdateEvent(@event, client, cancellationToken);
        }
        else if (@event.Action.IsDelete)
        {
            await OnMessageDeleteEvent(@event, client, cancellationToken);
        }
        else if (@event.Action.IsResume)
        {
            await OnMessageResumeEvent(@event, client, cancellationToken);
        }

        throw new Exception($"event '{@event.Key}' not found");
    }

    protected Task OnMessageCreateEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageUpdateEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageDeleteEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageResumeEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}