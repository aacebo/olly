using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;

using Octokit.GraphQL;

using OS.Agent.Cards.Progress;
using OS.Agent.Drivers.Github.Settings;
using OS.Agent.Errors;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

[Prompt]
[Prompt.Description(
    "an agent that can perform get/create/update/delete operations",
    "on Github data"
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that specializes in helping users manage their Github data.",
    "</agent>",
    "<authentication>",
        "Whenever you receive an error as a result of the user not being authenticated, use the SignIn method to prompt the user to sign in.",
    "</authentication>",
    "<tasks>",
        "You should break complex jobs into a series of incremental, single responsibility tasks.",
        "You are __REQUIRED__ to call StartTask whenever you start a new task.",
        "You are __REQUIRED__ to call EndTask whenever you complete an in progress task.",
    "</tasks>"
)]
public class GithubPrompt(Client client)
{
    public GithubSettings Settings => client.Provider.GetRequiredService<IOptions<GithubSettings>>().Value;

    [Function]
    [Function.Description("prompt the user to signin to their Github account")]
    public async Task<string> SignIn()
    {
        var state = new Token.State()
        {
            TenantId = client.Tenant.Id,
            UserId = client.User.Id,
            MessageId = client.Message.Id
        };

        await client.SignIn(Settings.InstallUrl, state.Encode());
        return "<user was prompted to login to Github>";
    }

    [Function]
    [Function.Description("Get the task list")]
    public string GetTasks()
    {
        return JsonSerializer.Serialize(client.Tasks, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("This function sends an update to the user indicating that you have started a new task.")]
    public async Task<string> StartTask([Param] string? title, [Param] string message)
    {
        var task = await client.SendTask(new()
        {
            Style = ProgressStyle.InProgress,
            Title = title,
            Message = message
        });

        return JsonSerializer.Serialize(task, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description(
        "This function sends an update to the user indicating that a specific task has completed.",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public async Task<string> EndTask([Param] Guid taskId, [Param] string? style, [Param] string? title, [Param] string? message)
    {
        var task = await client.SendTask(taskId, new()
        {
            Style = style is not null ? new(style) : null,
            Title = title,
            Message = message,
            EndedAt = DateTimeOffset.UtcNow
        });

        return JsonSerializer.Serialize(task, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a list of connected Github data source accounts for the user")]
    public async Task<string> GetAllGithubAccounts()
    {
        var task = await client.SendTask(new()
        {
            Title = "Github",
            Message = "fetching accounts..."
        });

        try
        {
            var accounts = await client.Services.Accounts.GetByTenantId(
                client.Tenant.Id,
                client.CancellationToken
            );

            await client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {accounts.Count()} accounts",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(accounts.Where(a => a.SourceType == SourceType.Github), client.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            await client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Error,
                EndedAt = DateTimeOffset.UtcNow
            });

            throw new Exception(ex.Message, ex);
        }
    }

    [Function]
    [Function.Description("get a list of the users Github repositories")]
    public async Task<string> GetRepositories()
    {
        var task = await client.SendTask(new()
        {
            Title = "Github",
            Message = "fetching repositories..."
        });

        try
        {
            var records = await client.Services.Records.GetByTenantId(
                client.Tenant.Id,
                Page.Create()
                    .Where("source_type", "=", SourceType.Github.ToString())
                    .Where("type", "=", "repository")
                    .Build(),
                client.CancellationToken
            );

            await client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {records.Count} repositories",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(records.List, client.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            await client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Error,
                EndedAt = DateTimeOffset.UtcNow
            });

            throw new Exception(ex.Message, ex);
        }
    }

    [Function]
    [Function.Description("get a list of a Github repositories discussions")]
    public async Task<string> GetRepositoryDiscussions([Param] Guid accountId, [Param] string repositoryName)
    {
        var task = await client.SendTask(new()
        {
            Title = "Github",
            Message = $"fetching discussions in repository {repositoryName}..."
        });

        try
        {
            var account = await client.Services.Accounts.GetById(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account not found");
            var install = await client.Services.Installs.GetByAccountId(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account install not found");
            var githubService = client.Provider.GetRequiredService<GithubService>();
            var github = await githubService.GetGraphConnection(install, client.CancellationToken);
            var query = new Octokit.GraphQL.Query()
                .RepositoryOwner(account.Name)
                .Repository(repositoryName)
                .Discussions()
                .AllPages()
                .Select(discussion => new
                {
                    discussion.Id,
                    discussion.Title,
                    discussion.Url,
                    discussion.Body
                })
                .Compile();

            var discussions = await github.Run(query, cancellationToken: client.CancellationToken);

            await client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {discussions.Count()} discussions in repository {repositoryName}",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(discussions, client.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            await client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Error,
                EndedAt = DateTimeOffset.UtcNow
            });

            throw new Exception(ex.Message, ex);
        }
    }
}