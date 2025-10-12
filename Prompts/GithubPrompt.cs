using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.Api.Activities;

using OS.Agent.Models;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description(
    "an agent that can perform get/create/update/delete operations",
    "on Github data"
)]
[Prompt.Instructions(
    "you are an agent that specializes in helping users manage their Github data.",
    "make sure to give incremental status updates to users via the Say function."
)]
public class GithubPrompt(IPromptContext context)
{
    [Function]
    [Function.Description("say something to the user")]
    public async Task Say([Param] string message)
    {
        await context.Send(message);
        await context.Send(new TypingActivity());
    }

    [Function]
    [Function.Description("get a list of connected data source accounts for the user")]
    public async Task<IEnumerable<Account>> GetAccounts()
    {
        return await context.Storage.Accounts.GetByTenantId(
            context.Tenant.Id,
            context.CancellationToken
        );
    }
}