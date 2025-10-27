using OS.Agent.Drivers.Teams.Events;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsWorker
{
    protected async Task OnInstallEvent(TeamsInstallEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        if (@event.Action.IsCreate)
        {
            await OnInstallCreateEvent(@event, client, cancellationToken);
        }
        else if (@event.Action.IsUpdate)
        {
            await OnInstallUpdateEvent(@event, client, cancellationToken);
        }
        else if (@event.Action.IsDelete)
        {
            await OnInstallDeleteEvent(@event, client, cancellationToken);
        }

        throw new Exception($"event '{@event.Key}' not found");
    }

    protected Task OnInstallCreateEvent(TeamsInstallEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnInstallUpdateEvent(TeamsInstallEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnInstallDeleteEvent(TeamsInstallEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}