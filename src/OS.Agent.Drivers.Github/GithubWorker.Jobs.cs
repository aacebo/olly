using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

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

    protected async Task OnJobCreateEvent(JobEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var services = provider.GetRequiredService<IServices>();
        var job = await services.Jobs.Update(@event.Job.Start(), cancellationToken);

        try
        {
            await services.Logs.Create(new()
            {
                TenantId = @event.Tenant.Id,
                Type = LogType.Job,
                TypeId = job.Id.ToString(),
                Text = "starting",
                Entities = job.Entities
            }, cancellationToken);

            // job logic...
            if (job.Name == "github.repository.index")
            {
                await OnIndexRepositoryJobEvent(@event, provider, cancellationToken);
            }

            await services.Logs.Create(new()
            {
                TenantId = @event.Tenant.Id,
                Type = LogType.Job,
                TypeId = job.Id.ToString(),
                Text = "stopping",
                Entities = job.Entities
            }, cancellationToken);

            await services.Jobs.Update(job.Success(), cancellationToken);
        }
        catch (Exception ex)
        {
            await services.Jobs.Update(job.Error(ex), cancellationToken);
        }
    }

    protected async Task OnJobUpdateEvent(JobEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var services = provider.GetRequiredService<IServices>();

        await services.Logs.Create(new()
        {
            TenantId = @event.Tenant.Id,
            Level = @event.Job.Status.IsError
                ? LogLevel.Error
                : LogLevel.Info,
            Type = LogType.Job,
            TypeId = @event.Job.Id.ToString(),
            Text = @event.Job.Message ?? @event.Job.Status,
            Entities = @event.Job.Entities
        }, cancellationToken);
    }

    protected async Task OnIndexRepositoryJobEvent(JobEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var services = provider.GetRequiredService<IServices>();
        var entity = @event.Job.Entities.GetRequired<GithubEntity>();
        var repository = entity.Repository ?? throw new InvalidOperationException("repository not found");
        var settings = entity.Settings ?? throw new InvalidOperationException("repository settings not found");
        var record = await services.Records.GetBySourceId(SourceType.Github, repository.NodeId, cancellationToken)
            ?? throw new InvalidOperationException("record not found");

        var githubService = provider.GetRequiredService<GithubService>();
        var github = new Octokit.GitHubClient(await githubService.GetRestConnection(@event.Install, cancellationToken));
        var res = await github.Repository.Content.GetAllContents(repository.Id);

        foreach (var item in res)
        {

        }
    }
}