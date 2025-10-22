using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Services;

public interface IRecordService
{
    Task<Record?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Record?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Record>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Record> Create(Record value, CancellationToken cancellationToken = default);
    Task<Record> Update(Record value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class RecordService(IServiceProvider provider) : IRecordService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<Event<RecordEvent>> Events { get; init; } = provider.GetRequiredService<NetMQQueue<Event<RecordEvent>>>();
    private IRecordStorage Storage { get; init; } = provider.GetRequiredService<IRecordStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();

    public async Task<Record?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var record = Cache.Get<Record>(id);

        if (record is not null)
        {
            return record;
        }

        record = await Storage.GetById(id, cancellationToken);

        if (record is not null)
        {
            Cache.Set(record.Id, record);
        }

        return record;
    }

    public async Task<PaginationResult<Record>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantId(tenantId, page, cancellationToken);
    }

    public async Task<Record?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var record = await Storage.GetBySourceId(tenantId, type, sourceId, cancellationToken);

        if (record is not null)
        {
            Cache.Set(record.Id, record);
        }

        return record;
    }

    public async Task<IEnumerable<Record>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByParentId(parentId, cancellationToken);
    }

    public async Task<Record> Create(Record value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var record = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new("records.create", new()
        {
            Tenant = tenant,
            Record = record
        }));

        return record;
    }

    public async Task<Record> Update(Record value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var record = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new("records.update", new()
        {
            Tenant = tenant,
            Record = record
        }));

        return record;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await GetById(id, cancellationToken) ?? throw new Exception("record not found");
        var tenant = await Tenants.GetById(record.TenantId, cancellationToken) ?? throw new Exception("tenant not found");

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new("records.delete", new()
        {
            Tenant = tenant,
            Record = record
        }));
    }
}