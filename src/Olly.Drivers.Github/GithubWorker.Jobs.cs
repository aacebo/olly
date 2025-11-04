using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.AI.Models.OpenAI.Extensions;

using Olly.Drivers.Github.Models;
using Olly.Events;
using Olly.Services;
using Olly.Storage.Models;

namespace Olly.Drivers.Github;

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

        if (@event.Attempt > 3)
        {
            return;
        }

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

            if (@event.Attempt < 3)
            {
                @event.Attempt++;
                Queue.Enqueue(@event);
            }
        }
    }

    protected async Task OnJobUpdateEvent(JobEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var services = provider.GetRequiredService<IServices>();

        await services.Logs.Create(new()
        {
            TenantId = @event.Tenant.Id,
            Level = @event.Job.Status.IsError
                ? Storage.Models.LogLevel.Error
                : Storage.Models.LogLevel.Info,
            Type = LogType.Job,
            TypeId = @event.Job.Id.ToString(),
            Text = @event.Job.Description ?? @event.Job.Status,
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

        var openaiSettings = provider.GetRequiredService<IConfiguration>().GetOpenAI();
        var openai = new OpenAI.OpenAIClient(openaiSettings.ApiKey).GetEmbeddingClient("text-embedding-3-small");
        var matcher = new Matcher();

        matcher.AddIncludePatterns(settings.Index.Include);
        matcher.AddExcludePatterns(settings.Index.Exclude);

        var githubService = provider.GetRequiredService<GithubService>();
        var github = new Octokit.GitHubClient(await githubService.GetRestConnection(@event.Install, cancellationToken));

        async Task Explore(string? path = null)
        {
            var items = path is null
                ? await github.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name)
                : await github.Repository.Content.GetAllContents(repository.Owner.Login, repository.Name, path);

            Logger.LogDebug("{} -> {}", path, items.Count);

            foreach (var item in items)
            {
                if (item.Type.Value != Octokit.ContentType.Dir && item.Type.Value != Octokit.ContentType.File)
                {
                    continue;
                }

                if (item.Type.Value == Octokit.ContentType.Dir)
                {
                    await Explore(item.Path);
                    continue;
                }

                if (!matcher.Match(item.Path).HasMatches)
                {
                    Logger.LogDebug("skipping {}...", item.Path);
                    continue;
                }

                var content = await github.Repository.Content.GetRawContent(repository.Owner.Login, repository.Name, item.Path);

                // max embeddings size for current model
                if (content.Length > 8285)
                {
                    Logger.LogWarning("file too large, skipping {}...", item.Path);
                    continue;
                }

                var contentUtf8 = Encoding.UTF8.GetString(content);

                if (string.IsNullOrEmpty(contentUtf8))
                {
                    Logger.LogDebug("skipping {}...", item.Path);
                    continue;
                }

                Logger.LogDebug("indexing {}...", item.Path);
                var document = await services.Documents.GetByPath(record.Id, item.Path, cancellationToken);

                if (document is null)
                {
                    var res = await openai.GenerateEmbeddingAsync(contentUtf8, new()
                    {
                        EndUserId = @event.User.Id.ToString()
                    }, cancellationToken);

                    await services.Documents.Create(new()
                    {
                        RecordId = record.Id,
                        Name = item.Name,
                        Path = item.Path,
                        Url = item.HtmlUrl,
                        Size = item.Size,
                        Encoding = item.Encoding,
                        Content = contentUtf8,
                        Embedding = new Pgvector.Vector(res.Value.ToFloats())
                    }, cancellationToken);
                }
            }
        }

        await Explore();
    }
}