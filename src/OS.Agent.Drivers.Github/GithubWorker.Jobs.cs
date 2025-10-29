using OS.Agent.Events;

namespace OS.Agent.Drivers.Github;

public partial class GithubWorker
{
    protected async Task OnJobEvent(JobEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        if (@event.Action.IsCreate)
        {
            await OnJobCreateEvent(@event, provider, cancellationToken);
            return;
        }
        else if (@event.Action.IsUpdate)
        {
            await OnJobUpdateEvent(@event, provider, cancellationToken);
            return;
        }

        throw new Exception($"event '{@event.Key}' not found");
    }

    protected Task OnJobCreateEvent(JobEvent @event, IServiceProvider provider, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnJobUpdateEvent(JobEvent @event, IServiceProvider provider, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }
}