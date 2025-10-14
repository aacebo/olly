using Microsoft.Teams.AI.Annotations;

using OS.Agent.Models;

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
}