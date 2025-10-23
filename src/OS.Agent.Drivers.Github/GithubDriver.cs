using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Drivers.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Github;

    private GithubService Github => provider.GetRequiredService<GithubService>();
    private IRecordService Records => provider.GetRequiredService<IRecordService>();

    public async Task Install(InstallRequest request, CancellationToken cancellationToken = default)
    {
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
                    await Records.Create(
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
                    await Records.Update(issueRecord, cancellationToken);
                }
            }
        }
    }

    public Task UnInstall(UnInstallRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SignIn(SignInRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}