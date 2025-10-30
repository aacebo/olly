using System.Text.Json;

using Microsoft.Teams.AI.Annotations;

using OS.Agent.Cards.Progress;
using OS.Agent.Drivers;

namespace OS.Agent.Prompts;

[Prompt("Olly")]
[Prompt.Description("An agent that delegates tasks to sub-agents")]
[Prompt.Instructions(
    "<agent>",
        "Your name is Olly.",
        "You are an agent that specializes in adding/managing/querying Data Sources for users.",
        "Anytime you receive a message you **MUST** use another agent to fetch the information needed to respond!",
        "Any answers you get from another agent should be titled with that agents name.",
        "This is so the user knows where the information is coming from, giving better context.",
    "</agent>",
    "<tasks>",
        "You should break complex jobs into a series of incremental, single responsibility tasks.",
        "You are __REQUIRED__ to call StartTask whenever you start a new task.",
        "You are __REQUIRED__ to call EndTask whenever you complete an in progress task.",
    "</tasks>"
)]
public class OllyPrompt(Client client)
{
    [Function]
    [Function.Description("Get the task list")]
    public string GetTasks()
    {
        return JsonSerializer.Serialize(client.Tasks, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("This function sends an update to the user indicating that you have started a new task.")]
    public async Task<string> StartTask([Param] string? title, [Param] string message)
    {
        var task = await client.SendTask(new()
        {
            Style = ProgressStyle.InProgress,
            Title = title,
            Message = message
        });

        return JsonSerializer.Serialize(task, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description(
        "This function sends an update to the user indicating that a specific task has completed.",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public async Task<string> EndTask([Param] Guid taskId, [Param] string? style, [Param] string? title, [Param] string? message)
    {
        var task = await client.SendTask(taskId, new()
        {
            Style = style is not null ? new(style) : null,
            Title = title,
            Message = message,
            EndedAt = DateTimeOffset.UtcNow
        });

        return JsonSerializer.Serialize(task, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("Get the current users chat information")]
    public Task<string> GetCurrentChat()
    {
        return Task.FromResult(JsonSerializer.Serialize(client.Chat, client.JsonSerializerOptions));
    }

    [Function]
    [Function.Description("Get the current users account information")]
    public Task<string> GetCurrentAccount()
    {
        return Task.FromResult(JsonSerializer.Serialize(client.Account, client.JsonSerializerOptions));
    }

    [Function]
    [Function.Description(
        "Get the current users chat history for this conversation.",
        "Messages with a role of 'assistant' were sent by you, any with role 'user' were ",
        "sent by the user!"
    )]
    public async Task<string> GetCurrentChatMessages([Param] int page = 1)
    {
        var res = await client.Services.Messages.GetByChatId(
            client.Chat.Id,
            Storage.Page.Create()
                .Index(page - 1)
                .Size(10)
                .Sort(Storage.SortDirection.Asc, "created_at")
                .Factory(q => q.WhereNotNull("account_id"))
                .Build(),
            client.CancellationToken
        );

        return JsonSerializer.Serialize(new
        {
            count = res.Count,
            page_count = res.TotalPages,
            page = res.Page,
            page_size = res.PerPage,
            data = res.List.Select(message => new
            {
                id = message.Id,
                role = message.AccountId is null ? "assistant" : "user",
                text = message.Text
            })
        }, client.JsonSerializerOptions);
    }
}