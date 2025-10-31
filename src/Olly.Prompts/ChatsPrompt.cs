using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Olly.Drivers;
using Olly.Storage;

namespace Olly.Prompts;

[Prompt("ChatsAgent")]
[Prompt.Description(
    "An agent that can fetch Chats.",
    "A Chat in Olly's database represents any external conversation.",
    "All chats have a source type that indicates where it was imported from."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that is an expert at Chat data retrieval.",
        "All chats have a source type that indicates where it was imported from.",
        "Chats are created in a Tenant.",
    "</agent>"
)]
public class ChatsPrompt
{
    private Client Client { get; }

    public static OpenAIChatPrompt Create(Client client, IServiceProvider provider)
    {
        var model = provider.GetRequiredService<OpenAIChatModel>();
        var logger = provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();

        return OpenAIChatPrompt.From(model, new ChatsPrompt(client), new()
        {
            Logger = logger
        });
    }

    public ChatsPrompt(Client client)
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
    [Function.Description("get a single Chat by its id")]
    public async Task<string?> GetChat([Param] Guid chatId)
    {
        var chat = await Client.Services.Chats.GetById(chatId, Client.CancellationToken);
        return chat is null ? null : JsonSerializer.Serialize(chat, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a page of Chats in the current Tenant")]
    public async Task<string> GetChats([Param] int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        var chats = await Client.Services.Chats.GetByTenantId(
            Client.Tenant.Id,
            Page.Create()
                .Index(page - 1)
                .Size(10)
                .Build(),
            Client.CancellationToken
        );

        return JsonSerializer.Serialize(chats, Client.JsonSerializerOptions);
    }
}