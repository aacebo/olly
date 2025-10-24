using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using OS.Agent.Contexts;
using OS.Agent.Drivers.Github.Settings;
using OS.Agent.Prompts.Github;
using OS.Agent.Storage.Models;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description("An agent that delegates tasks to sub-agents")]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that specializes in adding/managing/querying Data Sources for users.",
        "Anytime you receive a message you **MUST** use another agent to fetch the information needed to respond!",
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
public class MainPrompt
{
    public readonly AgentMessageContext Context;
    public readonly IOptions<GithubSettings> GithubSettings;
    public readonly OpenAIChatPrompt GithubPrompt;

    public MainPrompt(AgentMessageContext context)
    {
        Context = context;
        GithubSettings = context.Provider.GetRequiredService<IOptions<GithubSettings>>();
        GithubPrompt = OpenAIChatPrompt.From(context.Provider.GetRequiredService<OpenAIChatModel>(), new GithubPrompt(context), new()
        {
            Logger = context.Provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
        });
    }

    [Function]
    [Function.Description(
        "This function sends an update to user during a long process.",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public async Task SendUpdate([Param] string style, [Param] string? title, [Param] string? message = null)
    {
        await Context.SendProgressUpdate(style, title, message);
    }

    [Function]
    [Function.Description("get the current users chat information")]
    public Task<string> GetCurrentChat()
    {
        return Task.FromResult(JsonSerializer.Serialize(Context.Chat, Context.JsonSerializerOptions));
    }

    [Function]
    [Function.Description("get the current users account information")]
    public Task<string> GetCurrentAccount()
    {
        return Task.FromResult(JsonSerializer.Serialize(Context.Account, Context.JsonSerializerOptions));
    }

    [Function]
    [Function.Description(
        "delegate a task/question to the Github Agent ",
        "who specializes in Github subject matter."
    )]
    public async Task<string> GithubAgent([Param] string message)
    {
        var account = (await Context.Services.Accounts.GetByUserId(
            Context.User.Id,
            Context.CancellationToken
        )).FirstOrDefault(a => a.SourceType == SourceType.Github);

        var token = account is not null
            ? await Context.Services.Tokens.GetByAccountId(account.Id, Context.CancellationToken)
            : null;

        if (account is null || token is null)
        {
            var state = new Token.State()
            {
                TenantId = Context.Tenant.Id,
                UserId = Context.User.Id,
                MessageId = Context.Message.Id
            };

            await Context.SignIn(GithubSettings.Value.InstallUrl, state.Encode());
            return "<user was prompted to login to Github>";
        }

        await Context.Typing();

        var res = await GithubPrompt.Send(message, new()
        {
            Request = new()
            {
                Temperature = 0,
                EndUserId = Context.User.Id.ToString()
            }
        }, null, Context.CancellationToken);
        
        return res.Content;
    }
}