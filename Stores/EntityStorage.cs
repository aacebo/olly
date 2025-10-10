using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface IEntityStorage
{
    Task<Entity?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Entity?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entity>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Entity> Create(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Entity> Update(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class EntityStorage(ILogger<IEntityStorage> logger, QueryFactory db) : IEntityStorage
{
    public async Task<Entity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("entities")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Entity?>(cancellationToken: cancellationToken);
    }

    public async Task<Entity?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("entities")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Entity?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Entity>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByParentId");
        return await db
            .Query("entities")
            .Select("*")
            .Where("parent_id", "=", parentId)
            .GetAsync<Entity>(cancellationToken: cancellationToken);
    }

    public async Task<Entity> Create(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        await db.Query("entities").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task<Entity> Update(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("entities").Where("id", "=", value.Id).UpdateAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("entities").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}