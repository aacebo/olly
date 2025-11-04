using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Octokit.GraphQL;

using Olly.Drivers.Github.Settings;
using Olly.Errors;
using Olly.Prompts;
using Olly.Prompts.Extensions;
using Olly.Storage;
using Olly.Storage.Models;

namespace Olly.Drivers.Github.Prompts;

[Prompt("GithubAgent")]
[Prompt.Description(
    "An agent that can help answer questions, fetch data, or create/update/delete data in Github.",
    "GithubAgent supports adding a Github account type for the user."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that specializes in helping users manage their Github data.",
        "You are required to break down your work into incremental jobs, which should be managed via the JobsAgent.",
        "Jobs help you manage and communicate to the user complex tasks, you should create jobs whenever possible to communicate your thought process.",
        "All Jobs that are started must also be ended either as a success or error status!",
    "</agent>",
    "<authentication>",
        "Whenever you receive an error as a result of the user not being authenticated, use the SignIn method to prompt the user to sign in.",
    "</authentication>",
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
        .AddPrompt(RecordsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(JobsPrompt.Create(client, provider), cancellationToken);
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
    [Function.Description("get a list of connected Github data source accounts for the user")]
    public async Task<string> GetAllGithubAccounts()
    {
        var accounts = await Client.Services.Accounts.GetByTenantId(
            Client.Tenant.Id,
            Client.CancellationToken
        );

        return JsonSerializer.Serialize(accounts.Where(a => a.SourceType == SourceType.Github), Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a list of the users Github repositories")]
    public async Task<string> GetRepositories()
    {
        var records = await Client.Services.Records.GetByTenantId(
            Client.Tenant.Id,
            Page.Create()
                .Where("source_type", "=", SourceType.Github.ToString())
                .Where("type", "=", "repository")
                .Build(),
            Client.CancellationToken
        );

        return JsonSerializer.Serialize(records.List, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a list of a Github repositories discussions")]
    public async Task<string> GetRepositoryDiscussions([Param] Guid accountId, [Param] string repositoryName)
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
        return JsonSerializer.Serialize(discussions, Client.JsonSerializerOptions);
    }
}