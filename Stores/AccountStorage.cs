using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface IAccountStorage
{
    Task<Account?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByExternalId(SourceType type, string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByUserId(Guid userId, CancellationToken cancellationToken = default);
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

    public async Task<Account?> GetByExternalId(SourceType type, string externalId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByExternalId");
        return await db
            .Query("accounts")
            .Select("*")
            .Where("type", "=", type.ToString())
            .Where("external_id", "=", externalId)
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