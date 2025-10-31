using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Octokit.GraphQL;

using Olly.Cards.Progress;
using Olly.Drivers.Github.Settings;
using Olly.Errors;
using Olly.Prompts;
using Olly.Prompts.Extensions;
using Olly.Storage;
using Olly.Storage.Models;

namespace Olly.Drivers.Github;

[Prompt("GithubAgent")]
[Prompt.Description(
    "An agent that can help answer questions, fetch data, or create/update/delete data in Github.",
    "GithubAgent supports adding a Github account type for the user."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that specializes in helping users manage their Github data.",
    "</agent>",
    "<authentication>",
        "Whenever you receive an error as a result of the user not being authenticated, use the SignIn method to prompt the user to sign in.",
    "</authentication>",
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
public class GithubPrompt
{
    private GithubSettings Settings { get; }
    private Client Client { get; }

    public static OpenAIChatPrompt Create(Client client, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var model = provider.GetRequiredService<OpenAIChatModel>();
        var logger = provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();

        return OpenAIChatPrompt.From(model, new GithubPrompt(client), new()
        {
            Logger = logger
        })
        .AddPrompt(AccountsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(ChatsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(RecordsPrompt.Create(client, provider), cancellationToken);
    }

    public GithubPrompt(Client client)
    {
        Client = client;
        Settings = client.Provider.GetRequiredService<IOptions<GithubSettings>>().Value;
    }

    [Function]
    [Function.Description("prompt the user to signin to their Github account")]
    public async Task<string> SignIn()
    {
        if (Client.User is null || Client.Message is null)
        {
            throw new InvalidOperationException("cannot prompt user for sign in, no user or message present");
        }

        var installs = await Client.Services.Installs.GetByUserId(
            Client.User.Id,
            Storage.Query.Create()
                .Where("source_type", "=", SourceType.Github)
                .Build(),
            Client.CancellationToken
        );

        if (installs.Any(install => install.Status == InstallStatus.InProgress))
        {
            return "<user has already been prompted to install, wait patiently for them to sign in>";
        }

        if (installs.Any(install => install.Status == InstallStatus.Success))
        {
            return "<user already has a Github account installed>";
        }

        var state = new Token.State()
        {
            TenantId = Client.Tenant.Id,
            UserId = Client.User.Id,
            MessageId = Client.Message.Id
        };

        await Client.SignIn(Settings.InstallUrl, state.Encode());
        return "<user was prompted to login to Github>";
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
    [Function.Description("get a list of connected Github data source accounts for the user")]
    public async Task<string> GetAllGithubAccounts()
    {
        var task = await Client.SendTask(new()
        {
            Title = "Github",
            Message = "fetching accounts..."
        });

        try
        {
            var accounts = await Client.Services.Accounts.GetByTenantId(
                Client.Tenant.Id,
                Client.CancellationToken
            );

            await Client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {accounts.Count()} accounts",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(accounts.Where(a => a.SourceType == SourceType.Github), Client.JsonSerializerOptions);
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

    [Function]
    [Function.Description("get a list of the users Github repositories")]
    public async Task<string> GetRepositories()
    {
        var task = await Client.SendTask(new()
        {
            Title = "Github",
            Message = "fetching repositories..."
        });

        try
        {
            var records = await Client.Services.Records.GetByTenantId(
                Client.Tenant.Id,
                Page.Create()
                    .Where("source_type", "=", SourceType.Github.ToString())
                    .Where("type", "=", "repository")
                    .Build(),
                Client.CancellationToken
            );

            await Client.SendTask(task.Id, new()
            {
                Style = ProgressStyle.Success,
                Message = $"found {records.Count} repositories",
                EndedAt = DateTimeOffset.UtcNow
            });

            return JsonSerializer.Serialize(records.List, Client.JsonSerializerOptions);
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

    [Function]
    [Function.Description("get a list of a Github repositories discussions")]
    public async Task<string> GetRepositoryDiscussions([Param] Guid accountId, [Param] string repositoryName)
    {
        var task = await Client.SendTask(new()
        {
            Title = "Github",
            Message = $"fetching discussions in repository {repositoryName}..."
        });

        try
        {
            var account = await Client.Services.Accounts.GetById(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account not found");
            var install = await Client.Services.Installs.GetByAccountId(accountId) ?? throw HttpException.UnAuthorized().AddMessage("account install not found");
            var githubService = Client.Provider.GetRequiredService<GithubService>();
            var github = await githubService.GetGraphConnection(install, Client.CancellationToken);
            var query = new Octokit.GraphQL.Query()
                .RepositoryOwner(account.Name)
                .Repository(repositoryName)
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
                Message = $"found {discussions.Count()} discussions in repository {repositoryName}",
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