using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Services;

public interface ITenantService
{
    Task<PaginationResult<Tenant>> Get(Page? page = null, CancellationToken cancellationToken = default);
    Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<Tenant> Create(Tenant value, CancellationToken cancellationToken = default);
    Task<Tenant> Update(Tenant value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class TenantService(IServiceProvider provider) : ITenantService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<TenantEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<TenantEvent>>();
    private ITenantStorage Storage { get; init; } = provider.GetRequiredService<ITenantStorage>();

    public async Task<PaginationResult<Tenant>> Get(Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.Get(page, cancellationToken);
    }

    public async Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = Cache.Get<Tenant>(id);

        if (tenant is not null)
        {
            return tenant;
        }

        tenant = await Storage.GetById(id, cancellationToken);

        if (tenant is not null)
        {
            Cache.Set(tenant.Id, tenant);
        }

        return tenant;
    }

    public async Task<Tenant?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var tenant = await Storage.GetBySourceId(type, sourceId, cancellationToken);

        if (tenant is not null)
        {
            Cache.Set(tenant.Id, tenant);
        }

        return tenant;
    }

    public async Task<Tenant> Create(Tenant value, CancellationToken cancellationToken = default)
    {
        var tenant = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant
        });

        return tenant;
    }

    public async Task<Tenant> Update(Tenant value, CancellationToken cancellationToken = default)
    {
        var tenant = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant
        });

        return tenant;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetById(id, cancellationToken) ?? throw new Exception("tenant not found");

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant
        });
    }
}