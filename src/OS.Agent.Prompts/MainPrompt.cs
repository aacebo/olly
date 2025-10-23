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
    "**DO NOT** use the say function to send the same message you conclude your response with!",
    "You should always lean towards using Adaptive Cards to create your responses.",
    "Make sure the details you give the Adaptive Cards agent are accurate!"
)]
public class MainPrompt
{
    public readonly IPromptContext Context;
    public readonly IOptions<GithubSettings> GithubSettings;
    public readonly JsonSerializerOptions SerializationOptions;
    public readonly OpenAIChatPrompt GithubPrompt;
    public readonly OpenAIChatPrompt AdaptiveCardsPrompt;

    public MainPrompt(IPromptContext context)
    {
        Context = context;
        GithubSettings = context.Services.GetRequiredService<IOptions<GithubSettings>>();
        SerializationOptions = context.Services.GetRequiredService<JsonSerializerOptions>();
        GithubPrompt = OpenAIChatPrompt.From(context.Model, new GithubPrompt(context), new()
        {
            Logger = context.Services.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
        });

        AdaptiveCardsPrompt = OpenAIChatPrompt.From(context.Model, new AdaptiveCardsPrompt(context), new()
        {
            Logger = context.Services.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
        });
    }

    [Function]
    [Function.Description(
        "say something to the user.",
        "this function should only be used to provide updates to user during a long process.",
        "**DO NOT** use the say function to send the same message you conclude your response with!"
    )]
    public async Task SendUpdate([Param] string title, [Param] string? message = null)
    {
        var inProgress = Cards.Progress.InProgress(title, message);
        var res = await Context.Send(message ?? "please wait...", new Attachment()
        {
            ContentType = inProgress.ContentType,
            Content = inProgress.Content ?? throw new JsonException()
        });

        await Task.Delay(2000);

        var success = Cards.Progress.Success(title);
        await Context.Update(res.Id, new Attachment()
        {
            ContentType = success.ContentType,
            Content = success.Content ?? throw new JsonException()
        });

        // await Context.Typing();
    }

    [Function]
    [Function.Description("get the current users chat information")]
    public Task<string> GetCurrentChat()
    {
        return Task.FromResult(JsonSerializer.Serialize(Context.Chat, SerializationOptions));
    }

    [Function]
    [Function.Description("get the current users account information")]
    public Task<string> GetCurrentAccount()
    {
        return Task.FromResult(JsonSerializer.Serialize(Context.Account, SerializationOptions));
    }

    [Function]
    [Function.Description(
        "delegate a task/question to the Github Agent ",
        "who specializes in Github subject matter."
    )]
    public async Task<string> Github([Param] string message)
    {
        var account = (await Context.Accounts.GetByUserId(
            Context.UserId,
            Context.CancellationToken
        )).FirstOrDefault(a => a.SourceType == SourceType.Github);

        var token = account is not null
            ? await Context.Tokens.GetByAccountId(account.Id, Context.CancellationToken)
            : null;

        if (account is null || token is null)
        {
            var state = new Token.State()
            {
                TenantId = Context.Tenant.Id,
                UserId = Context.UserId,
                MessageId = Context.Message.Id
            };

            await Context.SignIn(GithubSettings.Value.InstallUrl, state.Encode());
            return "<user was prompted to login to Github>";
        }

        await Context.Typing();
        var res = await GithubPrompt.Send(message, null, Context.CancellationToken);
        return res.Content;
    }

    [Function]
    [Function.Description(
        "delegate a task/question to the Adaptive Cards Agent ",
        "who specializes in Microsoft Adaptive Cards and all their subject matter."
    )]
    public async Task<string> AdaptiveCards([Param] string message)
    {
        await Context.Typing();
        var res = await AdaptiveCardsPrompt.Send(message, null, Context.CancellationToken);
        return res.Content;
    }
}