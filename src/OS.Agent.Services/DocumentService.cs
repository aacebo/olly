using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Services;

public interface IDocumentService
{
    Task<Document?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Document?> GetByPath(Guid recordId, string path, CancellationToken cancellationToken = default);
    Task<PaginationResult<Document>> GetByRecordId(Guid recordId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> Search(Guid recordId, float[] embedding, int limit = 10, CancellationToken cancellationToken = default);
    Task<Document> Create(Document value, CancellationToken cancellationToken = default);
    Task<Document> Update(Document value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class DocumentService(IServiceProvider provider) : IDocumentService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<DocumentEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<DocumentEvent>>();
    private IDocumentStorage Storage { get; init; } = provider.GetRequiredService<IDocumentStorage>();
    private IRecordService Records { get; init; } = provider.GetRequiredService<IRecordService>();

    public async Task<Document?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var document = Cache.Get<Document>(id);

        if (document is not null)
        {
            return document;
        }

        document = await Storage.GetById(id, cancellationToken);

        if (document is not null)
        {
            Cache.Set(document.Id, document);
        }

        return document;
    }

    public async Task<Document?> GetByPath(Guid recordId, string path, CancellationToken cancellationToken = default)
    {
        var document = await Storage.GetByPath(recordId, path, cancellationToken);

        if (document is not null)
        {
            Cache.Set(document.Id, document);
        }

        return document;
    }

    public async Task<PaginationResult<Document>> GetByRecordId(Guid recordId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByRecordId(recordId, page, cancellationToken);
    }

    public async Task<IEnumerable<Document>> Search(Guid recordId, float[] embedding, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await Storage.Search(recordId, embedding, limit, cancellationToken);
    }

    public async Task<Document> Create(Document value, CancellationToken cancellationToken = default)
    {
        var record = await Records.GetById(value.RecordId, cancellationToken) ?? throw new Exception("record not found");
        var document = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Record = record,
            Document = document
        });

        return document;
    }

    public async Task<Document> Update(Document value, CancellationToken cancellationToken = default)
    {
        var record = await Records.GetById(value.RecordId, cancellationToken) ?? throw new Exception("record not found");
        var document = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Record = record,
            Document = document
        });

        return document;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetById(id, cancellationToken) ?? throw new Exception("document not found");
        var record = await Records.GetById(document.RecordId, cancellationToken) ?? throw new Exception("record not found");

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Record = record,
            Document = document
        });
    }
}