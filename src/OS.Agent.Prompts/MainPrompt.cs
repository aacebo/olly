using System.Text.Json;

using Microsoft.Teams.AI.Annotations;

using OS.Agent.Cards.Progress;
using OS.Agent.Drivers;

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
public class MainPrompt(Client client)
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
    [Function.Description("get the current users chat information")]
    public Task<string> GetCurrentChat()
    {
        return Task.FromResult(JsonSerializer.Serialize(client.Chat, client.JsonSerializerOptions));
    }

    [Function]
    [Function.Description("get the current users account information")]
    public Task<string> GetCurrentAccount()
    {
        return Task.FromResult(JsonSerializer.Serialize(client.Account, client.JsonSerializerOptions));
    }
}