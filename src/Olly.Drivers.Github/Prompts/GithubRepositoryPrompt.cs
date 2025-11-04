using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Octokit.GraphQL;

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
        .AddPrompt(RecordsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(JobsPrompt.Create(client, provider), cancellationToken);
    }

    public GithubRepositoryPrompt(Client client, Record repository)
    {
        Client = client;
        Repository = repository;
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
        return JsonSerializer.Serialize(discussions, Client.JsonSerializerOptions);
    }
}