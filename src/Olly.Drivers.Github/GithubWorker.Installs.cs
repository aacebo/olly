using Microsoft.Extensions.DependencyInjection;

using Olly.Cards.Progress;
using Olly.Cards.Tasks;
using Olly.Drivers.Github.Extensions;
using Olly.Drivers.Github.Models;
using Olly.Events;
using Olly.Storage.Models;

namespace Olly.Drivers.Github;

public partial class GithubWorker
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
        var githubService = client.Provider.GetRequiredService<GithubService>();
        var github = new Octokit.GitHubClient(await githubService.GetRestConnection(@event.Install, cancellationToken));

        try
        {
            install.Status = InstallStatus.InProgress;
            install = await client.Storage.Installs.Update(install, cancellationToken: cancellationToken);

            await client.Send("I see you've installed a new app, please wait while I import it...");

            var task = await client.SendTask(new()
            {
                Title = "Github",
                Message = "importing repositories..."
            });

            var repositories = await github.GitHubApps.Installation.GetAllRepositoriesForCurrent();

            // upsert installed repositories
            foreach (var repository in repositories.Repositories)
            {
                await OnInstallRepository(@event, client, github, repository, cancellationToken);
            }

            await client.SendTask(task.Id, new()
            {
                Title = "Github",
                Style = ProgressStyle.Success,
                Message = "importing success!",
                EndedAt = DateTimeOffset.UtcNow
            });

            install.Status = InstallStatus.Success;
            install = await client.Storage.Installs.Update(install, cancellationToken: cancellationToken);

            if (install.MessageId is not null)
            {
                await client.Services.Messages.Resume(install.MessageId.Value, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            install.Status = InstallStatus.Error;
            await client.Storage.Installs.Update(install, cancellationToken: cancellationToken);
            await client.Services.Logs.Create(new()
            {
                TenantId = @event.Tenant.Id,
                Level = LogLevel.Error,
                Type = LogType.Install,
                TypeId = install.Id.ToString(),
                Text = ex.Message
            }, cancellationToken);
        }
    }

    protected Task OnInstallUpdateEvent(InstallEvent @event, Client client, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnInstallDeleteEvent(InstallEvent @event, Client client, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected async Task OnInstallRepository(InstallEvent @event, Client client, Octokit.GitHubClient github, Octokit.Repository repository, CancellationToken cancellationToken = default)
    {
        var task = await client.SendTask(new()
        {
            Title = $"Github Repository: {repository.Name}",
            Message = "importing repository..."
        });

        var record = await client.Services.Records.GetBySourceId(SourceType.Github, repository.NodeId, cancellationToken);

        if (record is null)
        {
            record = await client.Services.Records.Create(
                @event.Account,
                new()
                {
                    SourceType = SourceType.Github,
                    SourceId = repository.NodeId,
                    Url = repository.HtmlUrl,
                    Type = "repository",
                    Name = repository.Name,
                    Entities = [new GithubEntity(repository)]
                },
                cancellationToken
            );
        }
        else
        {
            record.Name = repository.Name;
            record.Url = repository.HtmlUrl;
            record.Entities.Put(new GithubEntity(repository));
            record = await client.Services.Records.Update(record, cancellationToken);
        }

        var settings = await github.Repository.Content.GetOllySettings(repository.Owner.Login, repository.Name);

        if (settings is not null)
        {
            var entity = record.Entities.GetRequired<GithubEntity>();
            entity.Settings = settings;
            record = await client.Services.Records.Update(record, cancellationToken);

            await client.Services.Jobs.Create(new()
            {
                InstallId = @event.Install.Id,
                ChatId = @event.Chat?.Id,
                MessageId = @event.Message?.Id,
                Name = "github.repository.index",
                Entities = [entity]
            }, client.CancellationToken);
        }

        var issuesTask = await client.SendTask(new()
        {
            Title = $"Github Repository: {repository.Name}",
            Message = "importing issues..."
        });

        // upsert repository issues
        var issues = await github.Issue.GetAllForRepository(repository.Id);

        foreach (var issue in issues)
        {
            await OnInstallIssue(@event, issuesTask, record, client, github, repository, issue, cancellationToken);
        }

        await client.SendTask(issuesTask.Id, new()
        {
            Title = $"Github Repository: {repository.Name}",
            Style = ProgressStyle.Success,
            Message = "importing issues success!",
            EndedAt = DateTimeOffset.UtcNow
        });

        await client.SendTask(task.Id, new()
        {
            Title = $"Github Repository: {repository.Name}",
            Style = ProgressStyle.Success,
            Message = "importing repository success!",
            EndedAt = DateTimeOffset.UtcNow
        });
    }

    protected async Task OnInstallIssue(InstallEvent @event, TaskItem task, Record parent, Client client, Octokit.GitHubClient github, Octokit.Repository repository, Octokit.Issue issue, CancellationToken cancellationToken = default)
    {
        var record = await client.Services.Records.GetBySourceId(SourceType.Github, issue.NodeId, cancellationToken);

        if (record is null)
        {
            record = await client.Services.Records.Create(
                new()
                {
                    ParentId = parent.Id,
                    SourceType = SourceType.Github,
                    SourceId = issue.NodeId,
                    Url = issue.HtmlUrl,
                    Type = "issue",
                    Name = issue.Title,
                    Entities = [new GithubEntity(issue.ToUpdate())]
                },
                cancellationToken
            );
        }
        else
        {
            record.ParentId = parent.Id;
            record.Name = issue.Title;
            record.Url = issue.HtmlUrl;
            record.Entities = [new GithubEntity(issue.ToUpdate())];
            record = await client.Services.Records.Update(record, cancellationToken);
        }

        await client.SendTask(task.Id, new()
        {
            Title = $"Github Repository: {repository.Name}",
            Message = "importing issue comments..."
        });

        var comments = await github.Issue.Comment.GetAllForIssue(repository.Owner.Login, repository.Name, issue.Number);

        foreach (var comment in comments)
        {
            await OnInstallIssueComment(@event, record, client, comment, cancellationToken);
        }

        await client.SendTask(task.Id, new()
        {
            Title = $"Github Repository: {repository.Name}",
            Style = ProgressStyle.Success,
            Message = "importing issue comments success!"
        });
    }

    protected async Task OnInstallIssueComment(InstallEvent _, Record parent, Client client, Octokit.IssueComment comment, CancellationToken cancellationToken = default)
    {
        var record = await client.Services.Records.GetBySourceId(SourceType.Github, comment.NodeId, cancellationToken);

        if (record is null)
        {
            await client.Services.Records.Create(
                new()
                {
                    ParentId = parent.Id,
                    SourceType = SourceType.Github,
                    SourceId = comment.NodeId,
                    Url = comment.HtmlUrl,
                    Type = "issue.comment",
                    Entities = [new GithubEntity(comment)]
                },
                cancellationToken
            );
        }
        else
        {
            record.ParentId = parent.Id;
            record.Url = comment.HtmlUrl;
            record.Entities = [new GithubEntity(comment)];
            await client.Services.Records.Update(record, cancellationToken);
        }
    }
}