using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using OS.Agent.Drivers;

namespace OS.Agent.Prompts;

[Prompt("AccountsAgent")]
[Prompt.Description(
    "An agent that can fetch Accounts.",
    "An Account in Olly's database represents any external user or account.",
    "All accounts have a source type that indicates where it was imported from."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that is an expert at Account data retrieval.",
        "Accounts represents any external user or account.",
        "All accounts have a source type that indicates where it was imported from.",
        "Accounts are created in a Tenant.",
    "</agent>"
)]
public class AccountsPrompt
{
    private Client Client { get; }

    public static OpenAIChatPrompt Create(Client client, IServiceProvider provider)
    {
        var model = provider.GetRequiredService<OpenAIChatModel>();
        var logger = provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();

        return OpenAIChatPrompt.From(model, new AccountsPrompt(client), new()
        {
            Logger = logger
        });
    }

    public AccountsPrompt(Client client)
    {
        Client = client;
    }

    [Function]
    [Function.Description("get the current Tenant")]
    public string GetCurrentTenant()
    {
        return JsonSerializer.Serialize(Client.Tenant, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get the current User")]
    public string GetCurrentUser()
    {
        return JsonSerializer.Serialize(Client.User, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get the current User Account")]
    public string GetCurrentAccount()
    {
        return JsonSerializer.Serialize(Client.Account, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get the current Chat")]
    public string GetCurrentChat()
    {
        return JsonSerializer.Serialize(Client.Chat, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get an single Account by its id")]
    public async Task<string?> GetAccount([Param] Guid accountId)
    {
        var account = await Client.Services.Accounts.GetById(accountId, Client.CancellationToken);
        return account is null ? null : JsonSerializer.Serialize(account, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get all the Accounts in the current Tenant")]
    public async Task<string> GetTenantAccounts()
    {
        var accounts = await Client.Services.Accounts.GetByTenantId(Client.Tenant.Id, Client.CancellationToken);
        return JsonSerializer.Serialize(accounts, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get all the Accounts for the current User")]
    public async Task<string> GetUserAccounts()
    {
        if (Client.User is null)
        {
            throw new InvalidOperationException("user not found");
        }

        var accounts = await Client.Services.Accounts.GetByUserId(Client.User.Id, Client.CancellationToken);
        return JsonSerializer.Serialize(accounts, Client.JsonSerializerOptions);
    }
}