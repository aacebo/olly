using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Octokit.GraphQL;

using Olly.Cards.Progress;
using Olly.Prompts;
using Olly.Prompts.Extensions;
using Olly.Storage.Models;

namespace Olly.Drivers.Github.Prompts;

[Prompt("GithubRepositoryAgent")]
[Prompt.Description(
    "An agent that can help answer questions, fetch data, or create/update/delete data in a Github repository."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that specializes in helping users manage their Github repository issues/discussions/pull requests.",
        "Everything you do is scoped to the users current repository!",
        "Anytime the users asks a question or asks you to perform a task, they mean in the current repository!",
    "</agent>",
    "<tasks>",
        "You should break complex jobs into a series of incremental, single responsibility tasks.",
        "You are __REQUIRED__ to call StartTask whenever you start a new task.",
        "You are __REQUIRED__ to call EndTask whenever you complete an in progress task.",
    "</tasks>",
    "<search>",
        "When asked questions about a repository or its code/contents, ask the RecordsAgent for help!",
        "Github Discussions/Issues/PullRequests are represented as Chats in our system.",
        "Github Repositories are represented as Records in our system.",
        "Github Repository Contents (ie files/folders) are represented as Documents in our system",
    "</search>"
)]
public class GithubRepositoryPrompt
{
    private Client Client { get; }
    private Record Repository { get; }

    public static OpenAIChatPrompt Create(Client client, Record repository, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var model = provider.GetRequiredService<OpenAIChatModel>();
        var logger = provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();

        return OpenAIChatPrompt.From(model, new GithubRepositoryPrompt(client, repository), new()
        {
            Logger = logger
        })
        .AddPrompt(AccountsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(ChatsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(RecordsPrompt.Create(client, provider), cancellationToken);
    }

    public GithubRepositoryPrompt(Client client, Record repository)
    {
        Client = client;
        Repository = repository;
    }

    [Function]
    [Function.Description("Get the task list")]
    public string GetTasks()
    {
        return JsonSerializer.Serialize(Client.Tasks, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("This function sends an update to the user indicating that you have started a new task.")]
    public async Task<string> StartTask([Param] string? title, [Param] string message)
    {
        var task = await Client.SendTask(new()
        {
            Style = ProgressStyle.InProgress,
            Title = title,
            Message = message
        });

        return JsonSerializer.Serialize(task, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description(
        "This function sends an update to the user indicating that a specific task has completed.",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public async Task<string> EndTask([Param] Guid taskId, [Param] string? style, [Param] string? title, [Param] string? message)
    {
        var task = await Client.SendTask(taskId, new()
        {
            Style = style is not null ? new(style) : null,
            Title = title,
            Message = message,
            EndedAt = DateTimeOffset.UtcNow
        });

        return JsonSerializer.Serialize(task, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get the users current GitHub repository")]
    public string GetCurrentRepository()
    {
        return JsonSerializer.Serialize(Repository, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a list of a Github repositories discussions")]
    public async Task<string> GetRepositoryDiscussions()
    {
        var task = await Client.SendTask(new()
        {
            Title = "Github",
            Message = $"fetching discussions in repository {Repository.Name}..."
        });

        try
        {
            var githubService = Client.Provider.GetRequiredService<GithubService>();
            var github = await githubService.GetGraphConnection(Client.Install, Client.CancellationToken);
            var query = new Query()
                .RepositoryOwner(Client.Account.Name)
                .Repository(Repository.Name)
                .Discussions()
                .AllPages()
                .Select(discussion => new
                {
                    discussion.Id,
                    discussion.Title,
                    discussion.Url,
                    discussion.Body
                })
                .Compile();

            var discussions = await github.Run(query, cancellationToken: Client.CancellationToken);

            await Client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {discussions.Count()} discussions in repository {Repository.Name}",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(discussions, Client.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            await Client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Error,
                EndedAt = DateTimeOffset.UtcNow
            });

            throw new Exception(ex.Message, ex);
        }
    }
}