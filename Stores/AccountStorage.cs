using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface IAccountStorage
{
    Task<Account?> GetById(Guid id);
    Task<Account?> GetByExternalId(AccountType type, string externalId);
    Task<IEnumerable<Account>> GetByUserId(Guid userId);
    Task<Account> Create(Account value, IDbTransaction? tx = null);
    Task<Account> Update(Account value, IDbTransaction? tx = null);
    Task Delete(Guid id, IDbTransaction? tx = null);
}

public class AccountStorage(ILogger<IAccountStorage> logger, QueryFactory db) : IAccountStorage
{
    public async Task<Account?> GetById(Guid id)
    {
        logger.LogDebug("GetById");
        return await db
            .Query()
            .Select("*")
            .From("accounts")
            .Where("id", "=", id)
            .FirstAsync<Account>();
    }

    public async Task<Account?> GetByExternalId(AccountType type, string externalId)
    {
        logger.LogDebug("GetByExternalId");
        return await db
            .Query()
            .Select("*")
            .From("accounts")
            .Where("type", "=", type.ToString())
            .Where("external_id", "=", externalId)
            .FirstAsync<Account>();
    }

    public async Task<IEnumerable<Account>> GetByUserId(Guid userId)
    {
        logger.LogDebug("GetByUserId");
        return await db
            .Query()
            .Select("*")
            .From("accounts")
            .Where("user_id", "=", userId)
            .GetAsync<Account>();
    }

    public async Task<Account> Create(Account value, IDbTransaction? tx = null)
    {
        logger.LogDebug("Create");
        await db.Query("accounts").InsertAsync(value, tx);
        return value;
    }

    public async Task<Account> Update(Account value, IDbTransaction? tx = null)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("accounts").Where("id", "=", value.Id).InsertAsync(value, tx);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null)
    {
        logger.LogDebug("Delete");
        await db.Query("accounts").Where("id", "=", id).DeleteAsync(tx);
    }
}