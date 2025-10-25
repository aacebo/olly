using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Drivers.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubDriver
{
    public async Task Install(InstallRequest request, CancellationToken cancellationToken = default)
    {
        var driver = request.Chat is null
            ? null
            : provider.GetRequiredKeyedService<IChatDriver>(request.Chat.SourceType.ToString());

        if (driver is not null && request.Chat is not null)
        {
            await driver.Send(new()
            {
                Tenant = request.Tenant,
                User = request.User,
                Chat = request.Chat,
                Account = request.Account,
                Install = request.Install,
                Provider = request.Provider,
                Text = "⌛⌛⌛I see you've added a Github account, please wait while I import your data⌛⌛⌛"
            }, cancellationToken);
        }

        var client = new Octokit.GitHubClient(await Github.GetRestConnection(request.Install, cancellationToken));
        var repositories = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();

        // upsert installed repositories
        foreach (var repository in repositories.Repositories)
        {
            var record = await Records.GetBySourceId(SourceType.Github, repository.NodeId, cancellationToken);

            if (record is null)
            {
                record = await Records.Create(
                    request.Tenant,
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
                record = await Records.Update(record, cancellationToken);
            }

            // upsert repository issues
            var issues = await client.Issue.GetAllForRepository(repository.Id);

            foreach (var issue in issues)
            {
                var issueRecord = await Records.GetBySourceId(SourceType.Github, issue.NodeId, cancellationToken);

                if (issueRecord is null)
                {
                    issueRecord = await Records.Create(
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
                    issueRecord = await Records.Update(issueRecord, cancellationToken);
                }

                var comments = await client.Issue.Comment.GetAllForIssue(repository.Owner.Login, repository.Name, issue.Number);

                foreach (var comment in comments)
                {
                    var commentRecord = await Records.GetBySourceId(SourceType.Github, comment.NodeId, cancellationToken);

                    if (commentRecord is null)
                    {
                        await Records.Create(
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
                        await Records.Update(commentRecord, cancellationToken);
                    }
                }
            }
        }

        if (driver is not null && request.Chat is not null)
        {
            await driver.Send(new()
            {
                Tenant = request.Tenant,
                User = request.User,
                Chat = request.Chat,
                Account = request.Account,
                Install = request.Install,
                Provider = request.Provider,
                Text = "🎉🎉🎉Your Github account data has been successfully imported!🎉🎉🎉<br>Wnat can I assist you with?"
            }, cancellationToken);
        }
    }
}