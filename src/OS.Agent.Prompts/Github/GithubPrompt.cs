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
    "<updates>",
        "Make sure to give incremental status updates to users via the SendUpdate function.",
        "Status updates include any changes in your chain of thought.",
        "Several updates can be sent per single message sent by the user.",
        "**DO NOT** use the SendUpdate function to send the same message you conclude your response with!",
        "Call SendUpdate whenever you complete a unit of work.",
        "Send updates explaining your thought/reasoning as often as possible!",
        "You must send at least 5 updates per request.",
    "</updates>"
)]
public class GithubPrompt(AgentMessageContext context)
{
    [Function]
    [Function.Description(
        "This function sends an update to user during a long process.",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public async Task SendUpdate([Param] string style, [Param] string? title, [Param] string? message = null)
    {
        await context.SendProgressUpdate(style, title, message);
    }

    [Function]
    [Function.Description("get a list of connected Github data source accounts for the user")]
    public async Task<string> GetAllGithubAccounts()
    {
        await SendUpdate(ProgressStyle.InProgress, "Github", "fetching accounts...");

        var accounts = await context.Services.Accounts.GetByTenantId(
            context.Tenant.Id,
            context.CancellationToken
        );

        return JsonSerializer.Serialize(accounts.Where(a => a.SourceType == SourceType.Github), context.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a list of the users Github repositories")]
    public async Task<string> GetRepositories()
    {
        await SendUpdate(ProgressStyle.InProgress, "Github", "fetching repositories...");

        var records = await context.Services.Records.GetByTenantId(
            context.Tenant.Id,
            Page.Create()
                .Where("source_type", "=", SourceType.Github.ToString())
                .Where("type", "=", "repository")
                .Build(),
            context.CancellationToken
        );

        return JsonSerializer.Serialize(records.List, context.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a list of a Github repositories discussions")]
    public async Task<string> GetRepositoryDiscussions([Param] Guid accountId, [Param] string repositoryName)
    {
        await SendUpdate(ProgressStyle.InProgress, "Github", "fetching discussions...");

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
        return JsonSerializer.Serialize(discussions, context.JsonSerializerOptions);
    }
}