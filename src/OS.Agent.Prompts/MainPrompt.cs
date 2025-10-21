using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using OS.Agent.Drivers.Github;
using OS.Agent.Storage.Models;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description("An agent that delegates tasks to sub-agents")]
[Prompt.Instructions(
    "You are an agent that specializes in adding/managing/querying Data Sources for users.",
    "Make sure to give incremental status updates to users via the Say function.",
    "Status updates include any changes in your chain of thought.",
    "Several updates can be sent per single message sent by the user.",
    "You are an old british man, make sure you speek like one.",
    "**DO NOT** use the say function to send the same message you conclude your response with!"
)]
public class MainPrompt(IPromptContext context)
{
    public readonly IOptions<GithubSettings> GithubSettings = context.Services.GetRequiredService<IOptions<GithubSettings>>();
    public readonly JsonSerializerOptions SerializationOptions = context.Services.GetRequiredService<JsonSerializerOptions>();
    public readonly OpenAIChatPrompt GithubPrompt = OpenAIChatPrompt.From(context.Model, new GithubPrompt(context), new()
    {
        Logger = context.Services.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
    });

    [Function]
    [Function.Description(
        "say something to the user.",
        "this function should only be used to provide updates to user during a long process.",
        "**DO NOT** use the say function to send the same message you conclude your response with!"
    )]
    public async Task Say([Param] string message)
    {
        await context.Typing(message);
    }

    [Function]
    [Function.Description("get the current users chat information")]
    public Task<string> GetCurrentChat()
    {
        return Task.FromResult(JsonSerializer.Serialize(context.Chat, SerializationOptions));
    }

    [Function]
    [Function.Description("get the current users account information")]
    public Task<string> GetCurrentAccount()
    {
        return Task.FromResult(JsonSerializer.Serialize(context.Account, SerializationOptions));
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
                UserId = context.UserId,
                MessageId = context.Message.Id
            };

            await context.SignIn(GithubSettings.Value.InstallUrl, state.Encode());
            return "<user was prompted to login to Github>";
        }

        var res = await GithubPrompt.Send(message, null, context.CancellationToken);
        return res.Content;
    }
}