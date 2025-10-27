using OS.Agent.Drivers.Github.Events;

namespace OS.Agent.Drivers.Github;

public partial class GithubWorker
{
    protected async Task OnMessageEvent(GithubMessageEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.Action.IsCreate)
        {
            await OnMessageCreateEvent(@event, cancellationToken);
        }
        else if (@event.Action.IsUpdate)
        {
            await OnMessageUpdateEvent(@event, cancellationToken);
        }
        else if (@event.Action.IsDelete)
        {
            await OnMessageDeleteEvent(@event, cancellationToken);
        }
        else if (@event.Action.IsResume)
        {
            await OnMessageResumeEvent(@event, cancellationToken);
        }

        throw new Exception($"event '{@event.Key}' not found");
    }

    protected Task OnMessageCreateEvent(GithubMessageEvent @event, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageUpdateEvent(GithubMessageEvent @event, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageDeleteEvent(GithubMessageEvent @event, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageResumeEvent(GithubMessageEvent @event, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }
}