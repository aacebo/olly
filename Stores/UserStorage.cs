using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface IUserStorage
{
    Task<User?> GetById(Guid id);
    Task<User> Create(User value, IDbTransaction? tx = null);
    Task<User> Update(User value, IDbTransaction? tx = null);
    Task Delete(Guid id, IDbTransaction? tx = null);
}

public class UserStorage(ILogger<IAccountStorage> logger, QueryFactory db) : IUserStorage
{
    public async Task<User?> GetById(Guid id)
    {
        logger.LogDebug("GetById");
        return await db.Query()
            .Select("*")
            .From("users")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<User?>();
    }

    public async Task<User> Create(User value, IDbTransaction? tx = null)
    {
        logger.LogDebug("Create");
        await db.Query("users").InsertAsync(value, tx);
        return value;
    }

    public async Task<User> Update(User value, IDbTransaction? tx = null)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("users").UpdateAsync(value, tx);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null)
    {
        logger.LogDebug("Delete");
        await db.Query("users").Where("id", "=", id).DeleteAsync(tx);
    }
}