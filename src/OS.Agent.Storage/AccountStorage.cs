using System.Data;

using Microsoft.Extensions.Logging;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IAccountStorage
{
    Task<Account?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByUserId(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Account> Create(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Account> Update(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class AccountStorage(ILogger<IAccountStorage> logger, QueryFactory db) : IAccountStorage
{
    public async Task<Account?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("accounts")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Account?>(cancellationToken: cancellationToken);
    }

    public async Task<Account?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("accounts")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Account?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByUserId(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByUserId");
        return await db
            .Query()
            .Select("*")
            .From("accounts")
            .Where("user_id", "=", userId)
            .GetAsync<Account>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTenantId");
        return await db
            .Query()
            .Select("*")
            .From("accounts")
            .Where("tenant_id", "=", tenantId)
            .GetAsync<Account>(cancellationToken: cancellationToken);
    }

    public async Task<Account> Create(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        await db.Query("accounts").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task<Account> Update(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("accounts").Where("id", "=", value.Id).UpdateAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("accounts").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}