using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using OS.Agent.Cards.Progress;
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
    "<tasks>",
        "You should break complex jobs into a series of incremental, single responsibility tasks.",
        "You are __REQUIRED__ to call StartTask whenever you start a new task.",
        "You are __REQUIRED__ to call EndTask whenever you complete an in progress task.",
    "</tasks>"
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
    [Function.Description("Get the task list")]
    public string GetTasks()
    {
        return JsonSerializer.Serialize(Context.Tasks, Context.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("This function sends an update to the user indicating that you have started a new task.")]
    public async Task<string> StartTask([Param] string? title, [Param] string message)
    {
        var task = await Context.CreateTask(new()
        {
            Style = ProgressStyle.InProgress,
            Title = title,
            Message = message
        });

        return JsonSerializer.Serialize(task, Context.JsonSerializerOptions);
    }

    [Function]
    [Function.Description(
        "This function sends an update to the user indicating that a specific task has completed.",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public async Task<string> EndTask([Param] Guid taskId, [Param] string? style, [Param] string? title, [Param] string? message)
    {
        var task = await Context.UpdateTask(taskId, new()
        {
            Style = style is not null ? new(style) : null,
            Title = title,
            Message = message,
            EndedAt = DateTimeOffset.UtcNow
        });

        return JsonSerializer.Serialize(task, Context.JsonSerializerOptions);
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