using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsWorker
{
    protected async Task OnInstallEvent(TeamsInstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        if (@event.Action.IsCreate)
        {
            await OnInstallCreateEvent(@event, client, cancellationToken);
            return;
        }
        else if (@event.Action.IsUpdate)
        {
            await OnInstallUpdateEvent(@event, client, cancellationToken);
            return;
        }
        else if (@event.Action.IsDelete)
        {
            await OnInstallDeleteEvent(@event, client, cancellationToken);
            return;
        }

        throw new Exception($"event '{@event.Key}' not found");
    }

    protected async Task OnInstallCreateEvent(TeamsInstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        var install = @event.Install.Copy();
        install.Status = InstallStatus.Success;
        install = await client.Storage.Installs.Update(install, cancellationToken: cancellationToken);

        if (install.MessageId is not null)
        {
            await client.Services.Messages.Resume(install.MessageId.Value, cancellationToken);
        }
    }

    protected Task OnInstallUpdateEvent(TeamsInstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        return OnInstallCreateEvent(@event, client, cancellationToken);
    }

    protected Task OnInstallDeleteEvent(TeamsInstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}