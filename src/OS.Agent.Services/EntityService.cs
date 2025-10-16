using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Services;

public interface IEntityService
{
    Task<Entity?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Entity?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entity>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Entity> Create(Entity value, CancellationToken cancellationToken = default);
    Task<Entity> Update(Entity value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class EntityService(IServiceProvider provider) : IEntityService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<Event<EntityEvent>> Events { get; init; } = provider.GetRequiredService<NetMQQueue<Event<EntityEvent>>>();
    private IEntityStorage Storage { get; init; } = provider.GetRequiredService<IEntityStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();

    public async Task<Entity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var account = Cache.Get<Entity>(id);

        if (account is not null)
        {
            return account;
        }

        account = await Storage.GetById(id, cancellationToken);

        if (account is not null)
        {
            Cache.Set(account.Id, account);
        }

        return account;
    }

    public async Task<Entity?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var account = await Storage.GetBySourceId(tenantId, type, sourceId, cancellationToken);

        if (account is not null)
        {
            Cache.Set(account.Id, account);
        }

        return account;
    }

    public async Task<IEnumerable<Entity>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByParentId(parentId, cancellationToken);
    }

    public async Task<Entity> Create(Entity value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var account = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new("entities.create", new()
        {
            Tenant = tenant,
            Entity = account
        }));

        if (tenant.Name is null && value.Name != tenant.Name)
        {
            tenant.Name = account.Name;
            await Tenants.Update(tenant, cancellationToken);
        }

        return account;
    }

    public async Task<Entity> Update(Entity value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var account = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new("entities.update", new()
        {
            Tenant = tenant,
            Entity = account
        }));

        if (tenant.Name is null && value.Name != tenant.Name)
        {
            tenant.Name = account.Name;
            await Tenants.Update(tenant, cancellationToken);
        }

        return account;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await GetById(id, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new("entities.delete", new()
        {
            Tenant = tenant,
            Entity = account
        }));
    }
}