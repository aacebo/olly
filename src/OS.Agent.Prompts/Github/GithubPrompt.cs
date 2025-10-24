using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;

using Octokit.GraphQL;

using OS.Agent.Cards.Progress;
using OS.Agent.Contexts;
using OS.Agent.Drivers.Github;
using OS.Agent.Errors;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Prompts.Github;

[Prompt]
[Prompt.Description(
    "an agent that can perform get/create/update/delete operations",
    "on Github data"
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that specializes in helping users manage their Github data.",
    "</agent>",
    "<tasks>",
        "You should break complex jobs into a series of incremental, single responsibility tasks.",
        "You are __REQUIRED__ to call StartTask whenever you start a new task.",
        "You are __REQUIRED__ to call EndTask whenever you complete an in progress task.",
    "</tasks>"
)]
public class GithubPrompt(AgentMessageContext context)
{
    [Function]
    [Function.Description("Get the task list")]
    public string GetTasks()
    {
        return JsonSerializer.Serialize(context.Tasks, context.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("This function sends an update to the user indicating that you have started a new task.")]
    public async Task<string> StartTask([Param] string? title, [Param] string message)
    {
        var task = await context.CreateTask(new()
        {
            Style = ProgressStyle.InProgress,
            Title = title,
            Message = message
        });

        return JsonSerializer.Serialize(task, context.JsonSerializerOptions);
    }

    [Function]
    [Function.Description(
        "This function sends an update to the user indicating that a specific task has completed.",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public async Task<string> EndTask([Param] Guid taskId, [Param] string? style, [Param] string? title, [Param] string? message)
    {
        var task = await context.UpdateTask(taskId, new()
        {
            Style = style is not null ? new(style) : null,
            Title = title,
            Message = message,
            EndedAt = DateTimeOffset.UtcNow
        });

        return JsonSerializer.Serialize(task, context.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a list of connected Github data source accounts for the user")]
    public async Task<string> GetAllGithubAccounts()
    {
        var task = await context.CreateTask(new()
        {
            Title = "Github",
            Message = "fetching accounts..."
        });

        try
        {
            var accounts = await context.Services.Accounts.GetByTenantId(
                context.Tenant.Id,
                context.CancellationToken
            );

            await context.UpdateTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {accounts.Count()} accounts",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(accounts.Where(a => a.SourceType == SourceType.Github), context.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            await context.UpdateTask(task.Id, new()
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
        var task = await context.CreateTask(new()
        {
            Title = "Github",
            Message = "fetching repositories..."
        });

        try
        {
            var records = await context.Services.Records.GetByTenantId(
                context.Tenant.Id,
                Page.Create()
                    .Where("source_type", "=", SourceType.Github.ToString())
                    .Where("type", "=", "repository")
                    .Build(),
                context.CancellationToken
            );

            await context.UpdateTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {records.Count} repositories",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(records.List, context.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            await context.UpdateTask(task.Id, new()
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
        var task = await context.CreateTask(new()
        {
            Title = "Github",
            Message = $"fetching discussions in repository {repositoryName}..."
        });

        try
        {
            var account = await context.Services.Accounts.GetById(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account not found");
            var install = await context.Services.Installs.GetByAccountId(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account install not found");
            var github = context.Provider.GetRequiredService<GithubService>();
            var client = await github.GetGraphConnection(install, context.CancellationToken);
            var query = new Query()
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

            var discussions = await client.Run(query, cancellationToken: context.CancellationToken);

            await context.UpdateTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {discussions.Count()} discussions in repository {repositoryName}",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(discussions, context.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            await context.UpdateTask(task.Id, new()
            {
                Style = ProgressStyle.Error,
                EndedAt = DateTimeOffset.UtcNow
            });

            throw new Exception(ex.Message, ex);
        }
    }
}