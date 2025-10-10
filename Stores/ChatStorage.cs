using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface IChatStorage
{
    Task<Chat?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Chat?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Chat>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Chat> Create(Chat value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Chat> Update(Chat value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class ChatStorage(ILogger<IChatStorage> logger, QueryFactory db) : IChatStorage
{
    public async Task<Chat?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("chats")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Chat?>(cancellationToken: cancellationToken);
    }

    public async Task<Chat?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("chats")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Chat?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Chat>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByParentId");
        return await db
            .Query("chats")
            .Select("*")
            .Where("parent_id", "=", parentId)
            .GetAsync<Chat>(cancellationToken: cancellationToken);
    }

    public async Task<Chat> Create(Chat value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        await db.Query("chats").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task<Chat> Update(Chat value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("chats").Where("id", "=", value.Id).UpdateAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("chats").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}