using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description("an agent that delegates tasks to sub-agents")]
[Prompt.Instructions(
    "you are an agent that specializes in helping users all their data fetching/storage needs.",
    "make sure to give incremental status updates to users via the Say function."
)]
public class MainPrompt(IContext.Accessor accessor)
{
    private IContext<IActivity> Context => accessor.Value ?? throw new NullReferenceException("IContext<IActivity> is null");

    [Function]
    [Function.Description("say something to the user")]
    public async Task Say([Param] string message)
    {
        await Context.Send(message);
    }
}