using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;

using Octokit.GraphQL;
using Octokit.Internal;

using OS.Agent.Drivers.Github;
using OS.Agent.Drivers.Github.Models;
using OS.Agent.Errors;
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
    public async Task<string> GetRepositoriesByAccountId([Param] Guid accountId)
    {
        var account = await context.Accounts.GetById(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account not found");

        if (account.Data is GithubAccountInstallData data)
        {
            var adapter = new HttpClientAdapter(() => new GithubTokenRefreshHandler(context.Services, account));
            var connection = new Octokit.Connection(new Octokit.ProductHeaderValue("TOS-Agent"), adapter)
            {
                Credentials = new Octokit.Credentials(
                    data.AccessToken.Token,
                    Octokit.AuthenticationType.Bearer
                )
            };

            var client = new Octokit.GitHubClient(connection);
            var res = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();
            return JsonSerializer.Serialize(res.Repositories, SerializationOptions);
        }

        throw HttpException.UnAuthorized().AddMessage("account must be of type github");
    }

    [Function]
    [Function.Description("get a list of a Github repositories discussions")]
    public async Task<string> GetRepositoryDiscussions([Param] Guid accountId, [Param] string repositoryName)
    {
        var account = await context.Accounts.GetById(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account not found");
        var data = account.Data.GithubInstall() ?? throw HttpException.UnAuthorized();

        if (data.AccessToken.ExpiresAt >= DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            data.AccessToken = await context.AppGithub.GitHubApps.CreateInstallationToken(data.Install.Id);
            await context.Accounts.Update(account, context.CancellationToken);
        }

        var client = new Connection(
            new ProductHeaderValue("TOS-Agent"),
            data.AccessToken.Token
        );

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