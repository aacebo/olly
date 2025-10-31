using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Models.OpenAI.Extensions;

using Olly.Drivers;
using Olly.Storage;

namespace Olly.Prompts;

[Prompt("RecordsAgent")]
[Prompt.Description(
    "An agent that can query Records/Documents from Olly's database",
    "A Record in Olly's database can represent almost any data from an external system."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that is an expert at Record/Document data retrieval.",
        "Anytime you receive a request/question about code, check for relevant repositories/documents and answer accurately!",
    "</agent>",
    "<definitions>",
        "<record>represents any unit of data stored in Olly's database that was originally from an external system.</record>",
        "<document>represents any file under some Record and its contents/metadata</document>",
    "</definitions>"
)]
public class RecordsPrompt
{
    private Client Client { get; }
    private OpenAI.OpenAIClient OpenAI { get; }

    public static OpenAIChatPrompt Create(Client client, IServiceProvider provider)
    {
        var model = provider.GetRequiredService<OpenAIChatModel>();
        var logger = provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();

        return OpenAIChatPrompt.From(model, new RecordsPrompt(client), new()
        {
            Logger = logger
        });
    }

    public RecordsPrompt(Client client)
    {
        var openAiSettings = client.Provider.GetRequiredService<IConfiguration>().GetOpenAI();

        Client = client;
        OpenAI = new OpenAI.OpenAIClient(openAiSettings.ApiKey);
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
    [Function.Description("get a Record by its id")]
    public async Task<string?> GetRecordById([Param] Guid recordId)
    {
        var record = await Client.Services.Records.GetById(recordId, Client.CancellationToken);
        return record is null ? null : JsonSerializer.Serialize(record, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("Get a page of Records for a given tenantId.")]
    public async Task<string> GetRecordsByTenantId([Param] Guid tenantId, [Param] int page = 1)
    {
        var tenant = await Client.Services.Tenants.GetById(tenantId) ?? throw new Exception($"tenant with id {tenantId} not found");

        if (page < 1)
        {
            page = 1;
        }

        var res = await Client.Services.Records.GetByTenantId(
            tenant.Id,
            Page.Create()
                .Index(page - 1)
                .Size(10)
                .Build(),
            Client.CancellationToken
        );

        return JsonSerializer.Serialize(new
        {
            count = res.Count,
            page_count = res.TotalPages,
            page = res.Page,
            page_size = res.PerPage,
            data = res.List.Select(record => new
            {
                id = record.Id,
                type = "record",
                data = new
                {
                    id = record.Id,
                    parent_id = record.ParentId,
                    source_id = record.SourceId,
                    source_type = record.SourceType,
                    url = record.Url,
                    type = record.Type,
                    name = record.Name
                }
            })
        }, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a page of Records for a given accountId")]
    public async Task<string> GetRecordsByAccountId([Param] Guid accountId, [Param] int page = 1)
    {
        var account = await Client.Services.Accounts.GetById(accountId) ?? throw new Exception($"account with id {accountId} not found");

        if (page < 1)
        {
            page = 1;
        }

        var res = await Client.Services.Records.GetByAccountId(
            account.Id,
            Page.Create()
                .Index(page - 1)
                .Size(10)
                .Build(),
            Client.CancellationToken
        );

        return JsonSerializer.Serialize(new
        {
            count = res.Count,
            page_count = res.TotalPages,
            page = res.Page,
            page_size = res.PerPage,
            data = res.List.Select(record => new
            {
                id = record.Id,
                type = "record",
                data = new
                {
                    id = record.Id,
                    parent_id = record.ParentId,
                    source_id = record.SourceId,
                    source_type = record.SourceType,
                    url = record.Url,
                    type = record.Type,
                    name = record.Name
                }
            })
        }, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a page of Documents for a given recordId")]
    public async Task<string> GetDocumentsByRecordId([Param] Guid recordId, [Param] int page = 1)
    {
        var record = await Client.Services.Records.GetById(recordId) ?? throw new Exception($"record with id {recordId} not found");

        if (page < 1)
        {
            page = 1;
        }

        var res = await Client.Services.Documents.GetByRecordId(
            record.Id,
            Page.Create()
                .Index(page - 1)
                .Size(10)
                .Build(),
            Client.CancellationToken
        );

        return JsonSerializer.Serialize(new
        {
            count = res.Count,
            page_count = res.TotalPages,
            page = res.Page,
            page_size = res.PerPage,
            data = res.List.Select(document => new
            {
                id = document.Id,
                type = "document",
                data = new
                {
                    id = document.Id,
                    name = document.Name,
                    path = document.Path,
                    url = document.Url,
                    size = document.Size,
                    content = document.Content
                }
            })
        }, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("search a Records contents/documents/files")]
    public async Task<string> SearchRecordDocuments([Param] Guid recordId, [Param] string text)
    {
        var record = await Client.Services.Records.GetById(recordId, Client.CancellationToken) ?? throw new Exception("record not found");
        var client = OpenAI.GetEmbeddingClient("text-embedding-3-small");
        var res = await client.GenerateEmbeddingAsync(text, new()
        {
            EndUserId = Client.User?.Id.ToString()
        }, Client.CancellationToken);

        var documents = await Client.Services.Documents.Search(
            record.Id,
            res.Value.ToFloats().ToArray(),
            cancellationToken: Client.CancellationToken
        );

        return JsonSerializer.Serialize(documents.Select(document => new
        {
            id = document.Id,
            type = "document",
            data = new
            {
                id = document.Id,
                name = document.Name,
                path = document.Path,
                url = document.Url,
                size = document.Size,
                content = document.Content
            }
        }), Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("search for documents/files/code")]
    public async Task<string> Search([Param] string text)
    {
        var client = OpenAI.GetEmbeddingClient("text-embedding-3-small");
        var res = await client.GenerateEmbeddingAsync(text, new()
        {
            EndUserId = Client.User?.Id.ToString()
        }, Client.CancellationToken);

        var documents = await Client.Services.Documents.Search(
            res.Value.ToFloats().ToArray(),
            cancellationToken: Client.CancellationToken
        );

        return JsonSerializer.Serialize(documents.Select(document => new
        {
            id = document.Id,
            type = "document",
            data = new
            {
                id = document.Id,
                name = document.Name,
                path = document.Path,
                url = document.Url,
                size = document.Size,
                content = document.Content
            }
        }), Client.JsonSerializerOptions);
    }
}