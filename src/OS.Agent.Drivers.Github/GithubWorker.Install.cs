using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Cards.Progress;
using OS.Agent.Drivers.Github.Events;
using OS.Agent.Drivers.Github.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubWorker
{
    protected async Task OnInstallEvent(GithubInstallEvent @event, Client client, CancellationToken cancellationToken = default)
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

    protected async Task OnInstallCreateEvent(GithubInstallEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        var install = @event.Install.Copy();
        var githubService = @event.Scope.ServiceProvider.GetRequiredService<GithubService>();
        var services = @event.Scope.ServiceProvider.GetRequiredService<IServices>();
        var github = new Octokit.GitHubClient(await githubService.GetRestConnection(@event.Install, cancellationToken));

        try
        {
            install.Status = InstallStatus.InProgress;
            install = await client.Storage.Installs.Update(install, cancellationToken: cancellationToken);

            await client.Send("I see you've installed a new app, please wait while I import it...");

            var task = await client.SendTask(new()
            {
                Title = "Github",
                Message = "fetching repositories..."
            });

            var repositories = await github.GitHubApps.Installation.GetAllRepositoriesForCurrent();

            // upsert installed repositories
            foreach (var repository in repositories.Repositories)
            {
                var repositoryTask = await client.SendTask(new()
                {
                    Title = $"Github Repository: {repository.Name}",
                    Message = "importing repository..."
                });

                var record = await services.Records.GetBySourceId(SourceType.Github, repository.NodeId, cancellationToken);

                if (record is null)
                {
                    record = await services.Records.Create(
                        @event.Tenant,
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
                    record.Url = repository.Url;
                    record.Entities = [new GithubEntity(repository)];
                    record = await services.Records.Update(record, cancellationToken);
                }

                var issuesTask = await client.SendTask(new()
                {
                    Message = "fetching issues..."
                });

                // upsert repository issues
                var issues = await github.Issue.GetAllForRepository(repository.Id);

                foreach (var issue in issues)
                {
                    var issueRecord = await services.Records.GetBySourceId(SourceType.Github, issue.NodeId, cancellationToken);

                    if (issueRecord is null)
                    {
                        issueRecord = await services.Records.Create(
                            new()
                            {
                                ParentId = record.Id,
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
                        issueRecord.ParentId = record.Id;
                        issueRecord.Name = issue.Title;
                        issueRecord.Url = issue.HtmlUrl;
                        issueRecord.Entities = [new GithubEntity(issue.ToUpdate())];
                        issueRecord = await services.Records.Update(issueRecord, cancellationToken);
                    }

                    await client.SendTask(issuesTask.Id, new()
                    {
                        Message = "fetching comments..."
                    });

                    var comments = await github.Issue.Comment.GetAllForIssue(repository.Owner.Login, repository.Name, issue.Number);

                    foreach (var comment in comments)
                    {
                        var commentRecord = await services.Records.GetBySourceId(SourceType.Github, comment.NodeId, cancellationToken);

                        if (commentRecord is null)
                        {
                            await services.Records.Create(
                                new()
                                {
                                    ParentId = issueRecord.Id,
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
                            commentRecord.ParentId = issueRecord.Id;
                            commentRecord.Url = comment.HtmlUrl;
                            commentRecord.Entities = [new GithubEntity(comment)];
                            await services.Records.Update(commentRecord, cancellationToken);
                        }
                    }

                    await client.SendTask(issuesTask.Id, new()
                    {
                        Style = ProgressStyle.Success,
                        Message = "fetching issues success!"
                    });
                }

                await client.SendTask(repositoryTask.Id, new()
                {
                    Style = ProgressStyle.Success,
                    Message = "importing repository success!"
                });
            }

            await client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = "importing success!"
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

    protected Task OnInstallUpdateEvent(GithubInstallEvent @event, Client client, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnInstallDeleteEvent(GithubInstallEvent @event, Client client, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }
}