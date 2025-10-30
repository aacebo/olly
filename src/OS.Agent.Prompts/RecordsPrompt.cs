using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI.Extensions;

using OS.Agent.Drivers;
using OS.Agent.Storage;

namespace OS.Agent.Prompts;

[Prompt("RecordsPrompt")]
[Prompt.Description(
    "An agent that can query Records/Documents from Olly's database",
    "A Record in Olly's database can represent almost any data from an external system."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that is an expert Record/Document data retrieval.",
    "</agent>"
)]
public class RecordsPrompt
{
    private Client Client { get; }
    private OpenAI.OpenAIClient OpenAI { get; }

    public RecordsPrompt(Client client)
    {
        var openAiSettings = client.Provider.GetRequiredService<IConfiguration>().GetOpenAI();

        Client = client;
        OpenAI = new OpenAI.OpenAIClient(openAiSettings.ApiKey);
    }

    [Function]
    [Function.Description("get a Record by its id")]
    public async Task<string?> GetRecordById([Param] Guid recordId)
    {
        var record = await Client.Services.Records.GetById(recordId, Client.CancellationToken);
        return record is null ? null : JsonSerializer.Serialize(record, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a page of Records for a given tenantId")]
    public async Task<string> GetTenantRecords([Param] Guid tenantId, [Param] int page = 1)
    {
        var res = await Client.Services.Records.GetByTenantId(
            tenantId,
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
                parent_id = record.ParentId,
                source_id = record.SourceId,
                source_type = record.SourceType,
                url = record.Url,
                type = record.Type,
                name = record.Name
            })
        }, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("get a page of Records for a given accountId")]
    public async Task<string> GetAccountRecords([Param] Guid accountId, [Param] int page = 1)
    {
        var res = await Client.Services.Records.GetByAccountId(
            accountId,
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
                parent_id = record.ParentId,
                source_id = record.SourceId,
                source_type = record.SourceType,
                url = record.Url,
                type = record.Type,
                name = record.Name
            })
        }, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("search a Records documents")]
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

        return JsonSerializer.Serialize(documents, Client.JsonSerializerOptions);
    }
}