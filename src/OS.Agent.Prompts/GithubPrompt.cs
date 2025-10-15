using Microsoft.Teams.AI.Annotations;

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
    [Function]
    [Function.Description("get a list of connected Github data source accounts for the user")]
    public async Task<IEnumerable<Account>> GetAllGithubAccounts()
    {
        var accounts = await context.Accounts.GetByTenantId(
            context.Tenant.Id,
            context.CancellationToken
        );

        return accounts.Where(a => a.SourceType == SourceType.Github);
    }

    [Function]
    [Function.Description("get a list of the users Github repositories")]
    public async Task<IEnumerable<Octokit.Repository>> GetRepositoriesByAccountId([Param] Guid accountId)
    {
        var account = await context.Accounts.GetById(accountId) ?? throw new Exception("account not found");

        if (account.Data is GithubAccountData data)
        {
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("TOS-Agent"))
            {
                Credentials = new Octokit.Credentials(
                    data.AccessToken.Token,
                    Octokit.AuthenticationType.Bearer
                )
            };

            var res = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();
            return res.Repositories;
        }

        throw new UnauthorizedAccessException("account must be of type github");
    }
}