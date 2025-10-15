using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;

using OS.Agent.Drivers.Github;
using OS.Agent.Storage.Models;

using Api = Microsoft.Teams.Api;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description("An agent that delegates tasks to sub-agents")]
[Prompt.Instructions(
    "You are an agent that specializes in adding/managing/querying Data Sources for users.",
    "Make sure to give incremental status updates to users via the Say function.",
    "Status updates include any changes in your chain of thought.",
    "Several updates can be sent per single message sent by the user.",
    "You are an old british man, make sure you speek like one."
)]
public class MainPrompt(IPromptContext context)
{
    public readonly IOptions<GithubSettings> GithubSettings = context.Scope.ServiceProvider.GetRequiredService<IOptions<GithubSettings>>();
    public readonly OpenAIChatPrompt GithubPrompt = OpenAIChatPrompt.From(context.Model, new GithubPrompt(context), new()
    {
        Logger = context.Scope.ServiceProvider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
    });

    [Function]
    [Function.Description(
        "say something to the user.",
        "this function should only be used to provide updates to ",
        "user during a long process."
    )]
    public async Task Say([Param] string message)
    {
        await context.Send(new MessageActivity(message));
        await context.Send(new TypingActivity());
    }

    [Function]
    [Function.Description("get the current users chat information")]
    public Task<Chat> GetCurrentChat()
    {
        return Task.FromResult(context.Chat);
    }

    [Function]
    [Function.Description("get the current users account information")]
    public Task<Account> GetCurrentAccount()
    {
        return Task.FromResult(context.Account);
    }

    [Function]
    [Function.Description(
        "delegate a task/question to the Github Agent ",
        "who specializes in Github subject matter."
    )]
    public async Task<string> Github([Param] string message)
    {
        var account = (await context.Accounts.GetByUserId(
            context.UserId,
            context.CancellationToken
        )).FirstOrDefault(a => a.SourceType == SourceType.Github);

        var token = account is not null
            ? await context.Tokens.GetByAccountId(account.Id, context.CancellationToken)
            : null;

        if (account is null || token is null)
        {
            var state = new Token.State()
            {
                TenantId = context.Tenant.Id,
                UserId = context.UserId
            };

            await context.Send(
                new MessageActivity()
                {
                    InputHint = Api.InputHint.AcceptingInput,
                    Conversation = new()
                    {
                        Id = context.Chat.SourceId,
                        Type = Api.ConversationType.Personal
                    }
                }.AddAttachment(
                    Cards.Auth.SignIn($"{GithubSettings.Value.InstallUrl}&state={state.Encode()}")
                )
            );

            return "<user was prompted to login to Github>";
        }

        var res = await GithubPrompt.Send(message, null, context.CancellationToken);
        return res.Content;
    }
}