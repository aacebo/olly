using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface ITenantStorage
{
    Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<Tenant> Create(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Tenant> Update(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class TenantStorage(ILogger<ITenantStorage> logger, QueryFactory db) : ITenantStorage
{
    public async Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("tenants")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Tenant?>(cancellationToken: cancellationToken);
    }

    public async Task<Tenant?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("tenants")
            .Select("id", "sources", "name", "data", "created_at", "updated_at")
            .WhereRaw("sources @> ?::JSONB", $"[{{\"type\": \"{type}\", \"id\": \"{sourceId}\"}}]")
            .FirstOrDefaultAsync<Tenant?>(cancellationToken: cancellationToken);
    }

    public async Task<Tenant> Create(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        await db.Query("tenants").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task<Tenant> Update(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("tenants").Where("id", "=", value.Id).UpdateAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("tenants").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}