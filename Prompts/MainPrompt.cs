using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Cards;

using OS.Agent.Models;
using OS.Agent.Settings;

using Api = Microsoft.Teams.Api;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description("An agent that delegates tasks to sub-agents")]
[Prompt.Instructions(
    "Uou are an agent that specializes in adding/managing/querying Data Sources for users.",
    "Make sure to give incremental status updates to users via the Say function.",
    "Status updates include any changes in your chain of thought.",
    "Several updates can be sent per single message sent by the user."
)]
public class MainPrompt(IPromptContext context)
{
    public readonly IOptions<GithubSettings> GithubSettings = context.Scope.ServiceProvider.GetRequiredService<IOptions<GithubSettings>>();
    public readonly OpenAIChatPrompt GithubPrompt = OpenAIChatPrompt.From(context.Model, new GithubPrompt(context), new()
    {
        Logger = context.App.Logger
    });

    [Function]
    [Function.Description("say something to the user")]
    public async Task Say([Param] string message)
    {
        await context.Send(message);
        await context.Send(new TypingActivity());
    }

    [Function]
    [Function.Description("get the chat information")]
    public Task<Chat> GetChat()
    {
        return Task.FromResult(context.Chat);
    }

    [Function]
    [Function.Description("get the users account information")]
    public Task<Account> GetAccount()
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
        var accounts = await context.Storage.Accounts.GetByUserId(
            context.Account.UserId,
            context.CancellationToken
        );

        if (accounts.FirstOrDefault(a => a.SourceType == SourceType.Github) is null)
        {
            var state = new Token.State()
            {
                TenantId = context.Tenant.Id,
                UserId = context.Account.UserId
            };

            await context.Send(
                new MessageActivity()
                {
                    InputHint = Api.InputHint.AcceptingInput,
                    Conversation = context.Activity.Conversation
                }.AddAttachment(
                    new AdaptiveCard(
                        new Image("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR4ExGUTEwAQn95uM4KUU-OZ7Zz1n2lDrnXfw&s")
                            .WithHorizontalAlignment(HorizontalAlignment.Center)
                            .WithStyle(ImageStyle.RoundedCorners)
                            .WithSize(Size.Large),
                        new ActionSet(
                            new OpenUrlAction($"{GithubSettings.Value.OAuthUrl}&state={state.Encode()}")
                                .WithTitle("Login")
                                .WithStyle(ActionStyle.Positive)
                                .WithIconUrl("icon:ShieldLock")
                        ).WithHorizontalAlignment(HorizontalAlignment.Center)
                    )
                )
            );

            return "<user was prompted to login to Github>";
        }

        var res = await GithubPrompt.Send(message, null, context.CancellationToken);
        return res.Content;
    }
}