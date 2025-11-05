using Olly.Events;
using Olly.Storage.Models;

namespace Olly.Drivers.Teams;

public partial class TeamsWorker
{
    protected async Task OnInstallEvent(InstallEvent @event, Client client, CancellationToken cancellationToken = default)
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

    protected async Task OnInstallCreateEvent(InstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        var install = @event.Install.Copy();
        install.Status = InstallStatus.Success;
        await client.Storage.Installs.Update(install, cancellationToken: cancellationToken);
    }

    protected async Task OnInstallUpdateEvent(InstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        if (@event.Install.MessageId is not null)
        {
            await client.Services.Messages.Resume(@event.Install.MessageId.Value, cancellationToken);
        }
    }

    protected Task OnInstallDeleteEvent(InstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}