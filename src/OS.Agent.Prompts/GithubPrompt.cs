using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;

using Octokit.GraphQL;

using OS.Agent.Drivers.Github;
using OS.Agent.Errors;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description(
    "an agent that can perform get/create/update/delete operations",
    "on Github data"
)]
[Prompt.Instructions(
    "you are an agent that specializes in helping users manage their Github data."
)]
public class GithubPrompt(IPromptContext context)
{
    public JsonSerializerOptions SerializationOptions = context.Services.GetRequiredService<JsonSerializerOptions>();

    [Function]
    [Function.Description("get a list of connected Github data source accounts for the user")]
    public async Task<string> GetAllGithubAccounts()
    {
        var accounts = await context.Accounts.GetByTenantId(
            context.Tenant.Id,
            context.CancellationToken
        );

        return JsonSerializer.Serialize(accounts.Where(a => a.SourceType == SourceType.Github), SerializationOptions);
    }

    [Function]
    [Function.Description("get a list of the users Github repositories")]
    public async Task<string> GetRepositories()
    {
        var records = await context.Records.GetByTenantId(
            context.Tenant.Id,
            Page.Create()
                .Where("source_type", "=", SourceType.Github.ToString())
                .Where("type", "=", "repository")
                .Build(),
            context.CancellationToken
        );

        return JsonSerializer.Serialize(records.List, SerializationOptions);
    }

    [Function]
    [Function.Description("get a list of a Github repositories discussions")]
    public async Task<string> GetRepositoryDiscussions([Param] Guid accountId, [Param] string repositoryName)
    {
        var account = await context.Accounts.GetById(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account not found");
        var install = await context.Installs.GetByAccountId(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account install not found");
        var github = context.Services.GetRequiredService<GithubService>();
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
        return JsonSerializer.Serialize(discussions, SerializationOptions);
    }
}