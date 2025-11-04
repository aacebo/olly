using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Olly.Drivers;
using Olly.Prompts.Extensions;

namespace Olly.Prompts;

[Prompt("Olly")]
[Prompt.Description("An agent that delegates tasks to sub-agents")]
[Prompt.Instructions(
    "<agent>",
        "Your name is Olly.",
        "You are an agent that specializes in adding/managing/querying Data Sources for users.",
        "Anytime you receive a message you **MUST** use another agent to fetch the information needed to respond!",
        "Any answers you get from another agent should be titled with that agents name.",
        "This is so the user knows where the information is coming from, giving better context.",
    "</agent>"
)]
public class OllyPrompt(Client client)
{
    public static OpenAIChatPrompt Create(Client client, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var model = provider.GetRequiredService<OpenAIChatModel>();
        var logger = provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();

        return OpenAIChatPrompt.From(model, new OllyPrompt(client), new()
        {
            Logger = logger
        })
        .AddPrompt(AccountsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(ChatsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(RecordsPrompt.Create(client, provider), cancellationToken)
        .AddPrompt(JobsPrompt.Create(client, provider), cancellationToken);
    }

    [Function]
    [Function.Description("get the current Tenant")]
    public string GetCurrentTenant()
    {
        return JsonSerializer.Serialize(client.Tenant, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get the current User")]
    public string GetCurrentUser()
    {
        return JsonSerializer.Serialize(client.User, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get the current User Account")]
    public string GetCurrentAccount()
    {
        return JsonSerializer.Serialize(client.Account, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get the current Chat")]
    public string GetCurrentChat()
    {
        return JsonSerializer.Serialize(client.Chat, client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description(
        "Get the current users chat history for this conversation.",
        "Messages with a role of 'assistant' were sent by you, any with role 'user' were ",
        "sent by the user!"
    )]
    public async Task<string> GetCurrentChatMessages([Param] int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

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